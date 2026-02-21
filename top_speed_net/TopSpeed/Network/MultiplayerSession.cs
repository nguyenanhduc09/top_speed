using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed class MultiplayerSession : IDisposable
    {
        private readonly NetManager _manager;
        private readonly NetPeer _peer;
        private readonly IPEndPoint _serverEndPoint;
        private readonly CancellationTokenSource _cts;
        private readonly Task _pollTask;
        private readonly Task _keepAliveTask;
        private readonly ConcurrentQueue<IncomingPacket> _incoming;
        private byte _playerNumber;

        public MultiplayerSession(
            NetManager manager,
            NetPeer peer,
            IPEndPoint serverEndPoint,
            uint playerId,
            byte playerNumber,
            string? motd,
            string? playerName,
            ConcurrentQueue<IncomingPacket> incoming)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _peer = peer ?? throw new ArgumentNullException(nameof(peer));
            _serverEndPoint = serverEndPoint ?? throw new ArgumentNullException(nameof(serverEndPoint));
            _incoming = incoming ?? throw new ArgumentNullException(nameof(incoming));
            PlayerId = playerId;
            _playerNumber = playerNumber;
            Motd = motd ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            _cts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoop(_cts.Token));
            _keepAliveTask = Task.Run(() => KeepAliveLoop(_cts.Token));
        }

        public IPAddress Address => _serverEndPoint.Address;
        public int Port => _serverEndPoint.Port;
        public uint PlayerId { get; }
        public byte PlayerNumber => _playerNumber;
        public string Motd { get; }
        public string PlayerName { get; }

        public void UpdatePlayerNumber(byte playerNumber)
        {
            _playerNumber = playerNumber;
        }

        private void PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _manager.PollEvents();
                }
                catch
                {
                    // Ignore poll failures to keep the session alive.
                }

                Thread.Sleep(1);
            }
        }

        private async Task KeepAliveLoop(CancellationToken token)
        {
            var payload = new[] { ProtocolConstants.Version, (byte)Command.KeepAlive };
            while (!token.IsCancellationRequested)
            {
                SafeSend(payload, DeliveryMethod.Unreliable);

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public bool TryDequeuePacket(out IncomingPacket packet)
        {
            return _incoming.TryDequeue(out packet);
        }

        public void SendPlayerState(PlayerState state)
        {
            var payload = ClientPacketSerializer.WritePlayerState(Command.PlayerState, PlayerId, PlayerNumber, state);
            SafeSend(payload, DeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerData(PlayerRaceData raceData, CarType car, PlayerState state, bool engine, bool braking, bool horning, bool backfiring)
        {
            var payload = ClientPacketSerializer.WritePlayerDataToServer(PlayerId, PlayerNumber, car, raceData, state, engine, braking, horning, backfiring);
            SafeSend(payload, DeliveryMethod.Sequenced);
        }

        public void SendPlayerStarted()
        {
            var payload = ClientPacketSerializer.WritePlayer(Command.PlayerStarted, PlayerId, PlayerNumber);
            SafeSend(payload, DeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerFinished()
        {
            var payload = ClientPacketSerializer.WritePlayer(Command.PlayerFinished, PlayerId, PlayerNumber);
            SafeSend(payload, DeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerFinalize(PlayerState state)
        {
            var payload = ClientPacketSerializer.WritePlayerState(Command.PlayerFinalize, PlayerId, PlayerNumber, state);
            SafeSend(payload, DeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerCrashed()
        {
            var payload = ClientPacketSerializer.WritePlayer(Command.PlayerCrashed, PlayerId, PlayerNumber);
            SafeSend(payload, DeliveryMethod.ReliableOrdered);
        }

        public void SendRoomListRequest()
        {
            SafeSend(ClientPacketSerializer.WriteRoomListRequest(), DeliveryMethod.ReliableOrdered);
        }

        public void SendRoomCreate(string roomName, GameRoomType roomType, byte playersToStart)
        {
            SafeSend(ClientPacketSerializer.WriteRoomCreate(roomName, roomType, playersToStart), DeliveryMethod.ReliableOrdered);
        }

        public void SendRoomJoin(uint roomId)
        {
            SafeSend(ClientPacketSerializer.WriteRoomJoin(roomId), DeliveryMethod.ReliableOrdered);
        }

        public void SendRoomLeave()
        {
            SafeSend(ClientPacketSerializer.WriteRoomLeave(), DeliveryMethod.ReliableOrdered);
        }

        public void SendRoomSetTrack(string trackName)
        {
            SafeSend(ClientPacketSerializer.WriteRoomSetTrack(trackName), DeliveryMethod.ReliableOrdered);
        }

        public void SendRoomSetLaps(byte laps)
        {
            SafeSend(ClientPacketSerializer.WriteRoomSetLaps(laps), DeliveryMethod.ReliableOrdered);
        }

        public void SendRoomStartRace()
        {
            SafeSend(ClientPacketSerializer.WriteRoomStartRace(), DeliveryMethod.ReliableOrdered);
        }

        public void SendRoomSetPlayersToStart(byte playersToStart)
        {
            SafeSend(ClientPacketSerializer.WriteRoomSetPlayersToStart(playersToStart), DeliveryMethod.ReliableOrdered);
        }

        private void SafeSend(byte[] payload, DeliveryMethod deliveryMethod)
        {
            try
            {
                if (_peer.ConnectionState == ConnectionState.Connected)
                    _peer.Send(payload, deliveryMethod);
            }
            catch
            {
                // Ignore send failures to keep the client running.
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _pollTask.Wait(250); } catch { }
            try { _keepAliveTask.Wait(250); } catch { }
            _manager.Stop();
            _cts.Dispose();
        }
    }

    internal readonly struct IncomingPacket
    {
        public IncomingPacket(Command command, byte[] payload)
        {
            Command = command;
            Payload = payload;
        }

        public Command Command { get; }
        public byte[] Payload { get; }
    }
}
