namespace TopSpeed.Protocol
{
    public static class ProtocolConstants
    {
        public const int MaxPlayers = 10;
        public const int MaxMultiTrackLength = 8192;
        public const byte Version = 0x1F;
        public const int DefaultFrequency = 22050;
        public const int MaxPlayerNameLength = 24;
        public const int MaxMotdLength = 128;
        public const int MaxRoomNameLength = 32;
        public const int MaxRoomListEntries = 64;
        public const int MaxProtocolMessageLength = 96;
        public const int MaxRoomPlayersToStart = 10;
        public const string ConnectionKey = "TopSpeedMultiplayer";
    }
}
