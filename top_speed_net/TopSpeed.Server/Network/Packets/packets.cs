using System.Net;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void OnPacket(IPEndPoint endPoint, byte[] payload)
        {
            if (!PacketSerializer.TryReadHeader(payload, out var header))
            {
                _logger.Warning(LocalizationService.Format(
                    LocalizationService.Mark("Dropped packet with invalid header from {0}."),
                    endPoint));
                return;
            }
            if (header.Version != ProtocolConstants.Version)
            {
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Dropped packet with protocol version mismatch from {0}: received={1}, expected={2}."),
                    endPoint,
                    header.Version,
                    ProtocolConstants.Version));
                return;
            }

            lock (_lock)
            {
                var player = GetOrAddPlayer(endPoint);
                if (player == null)
                    return;

                player.LastSeenUtc = DateTime.UtcNow;
                if (HandlePendingHandshake(player, header.Command, payload, endPoint))
                    return;

                if (!_pktReg.TryDispatch(header.Command, player, payload, endPoint))
                    _logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("Ignoring unknown packet command {0} from {1}."),
                        (byte)header.Command,
                        endPoint));
            }
        }

        private void RegisterPackets()
        {
            RegisterCorePackets();
            RegisterRacePackets();
            RegisterMediaPackets();
            RegisterLivePackets();
            RegisterRoomPackets();
            RegisterChatPackets();
        }

    }
}
