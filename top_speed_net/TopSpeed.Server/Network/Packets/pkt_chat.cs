using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterChatPackets()
        {
            _pktReg.Add("chat", Command.ProtocolMessage, (player, payload, endPoint) =>
            {
                if (!PacketSerializer.TryReadProtocolMessage(payload, out var message))
                {
                    PacketFail(endPoint, Command.ProtocolMessage);
                    return;
                }

                HandleGlobalChat(player, message);
            });
        }

        private void HandleGlobalChat(PlayerConnection player, PacketProtocolMessage message)
        {
            if (player == null || message == null)
                return;

            switch (message.Code)
            {
                case ProtocolMessageCode.Chat:
                    BroadcastGlobalChat(player, message.Message);
                    break;
                case ProtocolMessageCode.RoomChat:
                    BroadcastRoomChat(player, message.Message);
                    break;
            }
        }
    }
}
