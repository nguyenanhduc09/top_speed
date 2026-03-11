using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerConnector
    {
        public async Task<ConnectResult> ConnectAsync(string host, int port, string callSign, TimeSpan timeout, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(host))
                return ConnectResult.CreateFail("No server address was provided.");

            var resolve = await Task.Run(() => TryResolveHost(host), token).ConfigureAwait(false);
            if (!resolve.Success)
                return ConnectResult.CreateFail(resolve.Error);

            var address = resolve.Address!;
            if (port <= 0 || port > 65535)
                port = ClientProtocol.DefaultServerPort;

            var sanitizedCallSign = SanitizeCallSign(callSign);
            var endpoint = new IPEndPoint(address, port);

            var incoming = new ConcurrentQueue<IncomingPacket>();
            var listener = new EventBasedNetListener();
            NetPeer? connectedPeer = null;
            var disconnected = false;
            var disconnectReason = string.Empty;

            listener.PeerConnectedEvent += peer => connectedPeer = peer;
            listener.PeerDisconnectedEvent += (_, info) =>
            {
                disconnected = true;
                disconnectReason = info.Reason.ToString();
                incoming.Enqueue(new IncomingPacket(
                    Command.Disconnect,
                    new[] { ProtocolConstants.Version, (byte)Command.Disconnect },
                    DateTime.UtcNow.Ticks));
            };
            listener.NetworkReceiveEvent += (_, reader, _, _) =>
            {
                var data = reader.GetRemainingBytes();
                reader.Recycle();
                if (ClientPacketSerializer.TryReadHeader(data, out var command))
                    incoming.Enqueue(new IncomingPacket(command, data, DateTime.UtcNow.Ticks));
            };

            var manager = new NetManager(listener)
            {
                UpdateTime = 1,
                ChannelsCount = PacketStreams.Count
            };

            if (!manager.Start())
                return ConnectResult.CreateFail("Failed to initialize network client.");

            manager.Connect(endpoint.Address.ToString(), endpoint.Port, ProtocolConstants.ConnectionKey);

            var protocolHello = BuildProtocolHelloPacket();
            var hello = BuildPlayerHelloPacket(sanitizedCallSign);
            var initialState = BuildPlayerStatePacket();
            var keepAlive = new[] { ProtocolConstants.Version, (byte)Command.KeepAlive };
            var protocolHelloSent = false;
            var protocolNegotiated = false;
            var nextKeepAliveUtc = DateTime.UtcNow;
            byte? playerNumber = null;
            uint? playerId = null;
            string? motd = null;
            PacketProtocolWelcome? protocolWelcome = null;
            string? protocolFailureMessage = null;
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
            {
                manager.PollEvents();

                if (disconnected && connectedPeer == null)
                {
                    manager.Stop();
                    return ConnectResult.CreateFail($"Connection failed: {disconnectReason}");
                }

                if (!protocolHelloSent && connectedPeer != null && connectedPeer.ConnectionState == ConnectionState.Connected)
                {
                    var sendHelloResult = TrySendHandshakePacket(connectedPeer, protocolHello);
                    if (!sendHelloResult.Success)
                    {
                        manager.Stop();
                        return ConnectResult.CreateFail(sendHelloResult.Error);
                    }

                    protocolHelloSent = true;
                    nextKeepAliveUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                }

                if (protocolHelloSent && connectedPeer != null && DateTime.UtcNow >= nextKeepAliveUtc)
                {
                    TrySendKeepAlive(connectedPeer, keepAlive);
                    nextKeepAliveUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                }

                var poll = ProcessConnectPoll(
                    incoming,
                    manager,
                    connectedPeer,
                    hello,
                    initialState,
                    protocolNegotiated,
                    playerId,
                    playerNumber,
                    motd,
                    protocolWelcome,
                    protocolFailureMessage,
                    disconnectReason,
                    sanitizedCallSign,
                    endpoint);

                protocolNegotiated = poll.ProtocolNegotiated;
                playerId = poll.PlayerId;
                playerNumber = poll.PlayerNumber;
                motd = poll.Motd;
                protocolWelcome = poll.ProtocolWelcome;
                protocolFailureMessage = poll.ProtocolFailureMessage;

                if (poll.Result.HasValue)
                    return poll.Result.Value;

                await Task.Delay(10, token).ConfigureAwait(false);
            }

            manager.Stop();
            if (protocolHelloSent && !protocolNegotiated)
                return ConnectResult.CreateFail("No protocol negotiation response from server. The server may be outdated or incompatible.");
            return ConnectResult.CreateFail("No response from server. The server may be offline or unreachable.");
        }

        private static ConnectPollState ProcessConnectPoll(
            ConcurrentQueue<IncomingPacket> incoming,
            NetManager manager,
            NetPeer? connectedPeer,
            byte[] hello,
            byte[] initialState,
            bool protocolNegotiated,
            uint? playerId,
            byte? playerNumber,
            string? motd,
            PacketProtocolWelcome? protocolWelcome,
            string? protocolFailureMessage,
            string disconnectReason,
            string sanitizedCallSign,
            IPEndPoint endpoint)
        {
            var state = new ConnectPollState
            {
                ProtocolNegotiated = protocolNegotiated,
                PlayerId = playerId,
                PlayerNumber = playerNumber,
                Motd = motd,
                ProtocolWelcome = protocolWelcome,
                ProtocolFailureMessage = protocolFailureMessage
            };

            var disconnectedDuringPoll = false;
            while (incoming.TryDequeue(out var packet))
            {
                if (packet.Command == Command.Disconnect)
                {
                    if (ClientPacketSerializer.TryReadDisconnect(packet.Payload, out var disconnectMessage) &&
                        !string.IsNullOrWhiteSpace(disconnectMessage))
                    {
                        state.ProtocolFailureMessage = disconnectMessage;
                    }

                    disconnectedDuringPoll = true;
                    continue;
                }

                if (packet.Command == Command.PlayerNumber && ClientPacketSerializer.TryReadPlayer(packet.Payload, out var assigned))
                {
                    if (!state.ProtocolNegotiated)
                    {
                        manager.Stop();
                        state.Result = ConnectResult.CreateFail("This server does not support required protocol negotiation. Please update your server.");
                        return state;
                    }

                    state.PlayerId = assigned.PlayerId;
                    state.PlayerNumber = assigned.PlayerNumber;
                    if (!string.IsNullOrWhiteSpace(state.Motd))
                    {
                        state.Result = ConnectResult.CreateSuccess(
                            manager,
                            connectedPeer,
                            endpoint,
                            assigned.PlayerId,
                            assigned.PlayerNumber,
                            state.Motd,
                            sanitizedCallSign,
                            incoming,
                            state.ProtocolWelcome);
                        return state;
                    }
                }
                else if (packet.Command == Command.ServerInfo && ClientPacketSerializer.TryReadServerInfo(packet.Payload, out var info))
                {
                    state.Motd = info.Motd;
                    if (state.PlayerId.HasValue && state.PlayerNumber.HasValue)
                    {
                        state.Result = ConnectResult.CreateSuccess(
                            manager,
                            connectedPeer,
                            endpoint,
                            state.PlayerId.Value,
                            state.PlayerNumber.Value,
                            state.Motd,
                            sanitizedCallSign,
                            incoming,
                            state.ProtocolWelcome);
                        return state;
                    }
                }
                else if (packet.Command == Command.ProtocolWelcome && ClientPacketSerializer.TryReadProtocolWelcome(packet.Payload, out var welcome))
                {
                    state.ProtocolWelcome = welcome;
                    if (!IsCompatibilityAccepted(welcome.Status))
                    {
                        state.ProtocolFailureMessage = string.IsNullOrWhiteSpace(welcome.Message)
                            ? "Connection refused due to protocol mismatch."
                            : welcome.Message;
                        disconnectedDuringPoll = true;
                        continue;
                    }

                    if (!state.ProtocolNegotiated && connectedPeer != null)
                    {
                        var helloResult = TrySendHandshakePacket(connectedPeer, hello);
                        if (!helloResult.Success)
                        {
                            manager.Stop();
                            state.Result = ConnectResult.CreateFail(helloResult.Error);
                            return state;
                        }

                        var stateResult = TrySendHandshakePacket(connectedPeer, initialState);
                        if (!stateResult.Success)
                        {
                            manager.Stop();
                            state.Result = ConnectResult.CreateFail(stateResult.Error);
                            return state;
                        }

                        state.ProtocolNegotiated = true;
                    }
                }
                else if (packet.Command == Command.ProtocolMessage && ClientPacketSerializer.TryReadProtocolMessage(packet.Payload, out var protocolMessage))
                {
                    if (protocolMessage.Code == ProtocolMessageCode.Failed)
                    {
                        state.ProtocolFailureMessage = string.IsNullOrWhiteSpace(protocolMessage.Message)
                            ? "Connection refused by server."
                            : protocolMessage.Message;
                        disconnectedDuringPoll = true;
                        continue;
                    }
                }
            }

            if (disconnectedDuringPoll)
            {
                manager.Stop();
                state.Result = BuildDisconnectedResult(state.ProtocolFailureMessage, state.ProtocolWelcome, disconnectReason);
                return state;
            }

            if (state.ProtocolNegotiated && state.PlayerId.HasValue && state.PlayerNumber.HasValue && connectedPeer != null)
            {
                state.Result = ConnectResult.CreateSuccess(
                    manager,
                    connectedPeer,
                    endpoint,
                    state.PlayerId.Value,
                    state.PlayerNumber.Value,
                    state.Motd,
                    sanitizedCallSign,
                    incoming,
                    state.ProtocolWelcome);
            }

            return state;
        }

        private static ConnectResult BuildDisconnectedResult(
            string? protocolFailureMessage,
            PacketProtocolWelcome? protocolWelcome,
            string disconnectReason)
        {
            if (!string.IsNullOrWhiteSpace(protocolFailureMessage))
                return ConnectResult.CreateFail(protocolFailureMessage!);

            if (protocolWelcome != null && !IsCompatibilityAccepted(protocolWelcome.Status))
            {
                var fallback = BuildProtocolRefusalFallback(protocolWelcome);
                return ConnectResult.CreateFail(fallback);
            }

            var reason = string.IsNullOrWhiteSpace(disconnectReason)
                ? "The server refused the connection."
                : $"The server refused the connection. Details: {disconnectReason}.";
            return ConnectResult.CreateFail(reason);
        }

        private struct ConnectPollState
        {
            public bool ProtocolNegotiated;
            public uint? PlayerId;
            public byte? PlayerNumber;
            public string? Motd;
            public PacketProtocolWelcome? ProtocolWelcome;
            public string? ProtocolFailureMessage;
            public ConnectResult? Result;
        }
    }
}
