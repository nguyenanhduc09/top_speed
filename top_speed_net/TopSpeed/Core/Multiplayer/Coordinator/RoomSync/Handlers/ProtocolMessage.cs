using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleProtocolMessage(PacketProtocolMessage message)
        {
            _chatFlow.HandleProtocolMessage(message);
        }

        internal void HandleProtocolMessageCore(PacketProtocolMessage message)
        {
            if (message == null)
                return;

            var localizedMessage = string.IsNullOrWhiteSpace(message.Message)
                ? string.Empty
                : LocalizationService.Translate(message.Message);

            var effects = new List<PacketEffect>();
            AddProtocolMessageEffects(message, localizedMessage, effects);

            if (!string.IsNullOrWhiteSpace(localizedMessage))
                effects.Add(PacketEffect.Speak(localizedMessage));

            DispatchPacketEffects(effects);
        }

        private static void AddProtocolMessageEffects(PacketProtocolMessage message, string localizedMessage, List<PacketEffect> effects)
        {
            switch (message.Code)
            {
                case ProtocolMessageCode.ServerPlayerConnected:
                    effects.Add(PacketEffect.PlaySound("online.ogg"));
                    effects.Add(PacketEffect.AddConnectionHistory(localizedMessage));
                    break;

                case ProtocolMessageCode.ServerPlayerDisconnected:
                    effects.Add(PacketEffect.PlaySound("offline.ogg"));
                    effects.Add(PacketEffect.AddConnectionHistory(localizedMessage));
                    break;

                case ProtocolMessageCode.Chat:
                    effects.Add(PacketEffect.PlaySound("chat.ogg"));
                    effects.Add(PacketEffect.AddGlobalChatHistory(localizedMessage));
                    break;

                case ProtocolMessageCode.RoomChat:
                    effects.Add(PacketEffect.PlaySound("room_chat.ogg"));
                    effects.Add(PacketEffect.AddRoomChatHistory(localizedMessage));
                    break;

                default:
                    effects.Add(PacketEffect.AddRoomEventHistory(localizedMessage));
                    break;
            }
        }
    }
}

