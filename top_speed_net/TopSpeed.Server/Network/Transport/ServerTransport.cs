using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using TopSpeed.Protocol;
using TopSpeed.Localization;
using TopSpeed.Server.Logging;

namespace TopSpeed.Server.Network
{
    internal sealed class UdpServerTransport : IDisposable
    {
        private readonly Logger _logger;
        private readonly object _peerLock = new object();
        private EventBasedNetListener? _listener;
        private NetManager? _server;
        private readonly Dictionary<string, NetPeer> _peers = new Dictionary<string, NetPeer>(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _cts;
        private Task? _pollTask;

        public event Action<IPEndPoint, byte[]>? PacketReceived;
        public event Action<IPEndPoint>? PeerDisconnected;

        public UdpServerTransport(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start(int port)
        {
            if (_server != null)
                return;

            _listener = new EventBasedNetListener();
            _listener.ConnectionRequestEvent += request => request.AcceptIfKey(ProtocolConstants.ConnectionKey);
            _listener.PeerConnectedEvent += peer =>
            {
                lock (_peerLock)
                    _peers[GetPeerKey(peer)] = peer;
            };
            _listener.PeerDisconnectedEvent += (peer, _) =>
            {
                var endpoint = CreatePeerEndpoint(peer);
                lock (_peerLock)
                    _peers.Remove(GetPeerKey(peer));
                PeerDisconnected?.Invoke(endpoint);
            };
            _listener.NetworkReceiveEvent += (peer, reader, _, _) =>
            {
                var buffer = reader.GetRemainingBytes();
                reader.Recycle();
                PacketReceived?.Invoke(CreatePeerEndpoint(peer), buffer);
            };

            _server = new NetManager(_listener)
            {
                ReuseAddress = true,
                UpdateTime = 1,
                ChannelsCount = PacketStreams.Count
            };

            if (!_server.Start(port))
                throw new InvalidOperationException(LocalizationService.Format(
                    LocalizationService.Mark("Failed to start transport on port {0}."),
                    port));

            _cts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoop(_cts.Token));
            _logger.Info(LocalizationService.Format(LocalizationService.Mark("LiteNetLib transport listening on {0}."), port));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _pollTask?.Wait(250);
            _pollTask = null;
            _cts?.Dispose();
            _cts = null;

            _server?.Stop();
            _server = null;
            lock (_peerLock)
                _peers.Clear();
            _listener = null;
            _logger.Info(LocalizationService.Mark("LiteNetLib transport stopped."));
        }

        public void Send(IPEndPoint endPoint, byte[] payload, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            Send(endPoint, payload, deliveryMethod, channel: 0);
        }

        public void Send(IPEndPoint endPoint, byte[] payload, DeliveryMethod deliveryMethod, byte channel)
        {
            if (_server == null || payload == null || payload.Length == 0)
                return;

            NetPeer? peer;
            lock (_peerLock)
                _peers.TryGetValue(endPoint.ToString(), out peer);

            if (peer == null || peer.ConnectionState != ConnectionState.Connected)
                return;

            try
            {
                peer.Send(payload, channel, deliveryMethod);
            }
            catch (Exception ex)
            {
                _logger.Warning(LocalizationService.Format(
                    LocalizationService.Mark("LiteNetLib send failed: {0}"),
                    ex.Message));
            }
        }

        private void PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _server?.PollEvents();
                }
                catch (Exception ex)
                {
                    _logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("LiteNetLib poll failed: {0}"),
                        ex.Message));
                }

                Thread.Sleep(1);
            }
        }

        private static string GetPeerKey(NetPeer peer)
        {
            return $"{peer.Address}:{peer.Port}";
        }

        private static IPEndPoint CreatePeerEndpoint(NetPeer peer)
        {
            return new IPEndPoint(peer.Address, peer.Port);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
