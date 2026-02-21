using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static class ClientPacketSerializer
    {
        public static bool TryReadHeader(byte[] data, out Command command)
        {
            command = Command.Disconnect;
            if (data.Length < 2)
                return false;
            if (data[0] != ProtocolConstants.Version)
                return false;
            command = (Command)data[1];
            return true;
        }

        public static bool TryReadPlayer(byte[] data, out PacketPlayer packet)
        {
            packet = new PacketPlayer();
            if (data.Length < 2 + 4 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            return true;
        }

        public static bool TryReadPlayerState(byte[] data, out PacketPlayerState packet)
        {
            packet = new PacketPlayerState();
            if (data.Length < 2 + 4 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.State = (PlayerState)reader.ReadByte();
            return true;
        }

        public static bool TryReadPlayerData(byte[] data, out PacketPlayerData packet)
        {
            packet = new PacketPlayerData();
            if (data.Length < 2 + 4 + 1 + 1 + 4 + 4 + 2 + 4 + 1 + 1 + 1 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.Car = (CarType)reader.ReadByte();
            packet.RaceData.PositionX = reader.ReadSingle();
            packet.RaceData.PositionY = reader.ReadSingle();
            packet.RaceData.Speed = reader.ReadUInt16();
            packet.RaceData.Frequency = reader.ReadInt32();
            packet.State = (PlayerState)reader.ReadByte();
            packet.EngineRunning = reader.ReadBool();
            packet.Braking = reader.ReadBool();
            packet.Horning = reader.ReadBool();
            packet.Backfiring = reader.ReadBool();
            return true;
        }

        public static bool TryReadPlayerBumped(byte[] data, out PacketPlayerBumped packet)
        {
            packet = new PacketPlayerBumped();
            if (data.Length < 2 + 4 + 1 + 4 + 4 + 2)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.BumpX = reader.ReadSingle();
            packet.BumpY = reader.ReadSingle();
            packet.BumpSpeed = reader.ReadUInt16();
            return true;
        }

        public static bool TryReadLoadCustomTrack(byte[] data, out PacketLoadCustomTrack packet)
        {
            packet = new PacketLoadCustomTrack();
            const int headerSize = 2;
            const int baseSize = 1 + 12 + 1 + 1 + 2;
            if (data.Length < headerSize + baseSize)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.LoadCustomTrack)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.NrOfLaps = reader.ReadByte();
            packet.TrackName = reader.ReadFixedString(12);
            packet.TrackWeather = (TrackWeather)reader.ReadByte();
            packet.TrackAmbience = (TrackAmbience)reader.ReadByte();
            packet.TrackLength = reader.ReadUInt16();
            var availableDefs = Math.Max(0, (data.Length - headerSize - baseSize) / 7);
            var definitionCount = Math.Min(packet.TrackLength, (ushort)availableDefs);
            var definitions = new TrackDefinition[definitionCount];
            for (var i = 0; i < definitionCount; i++)
            {
                var type = (TrackType)reader.ReadByte();
                var surface = (TrackSurface)reader.ReadByte();
                var noise = (TrackNoise)reader.ReadByte();
                var segmentLength = reader.ReadSingle();
                definitions[i] = new TrackDefinition(type, surface, noise, segmentLength);
            }

            packet.Definitions = definitions;
            return true;
        }

        public static bool TryReadServerInfo(byte[] data, out PacketServerInfo packet)
        {
            packet = new PacketServerInfo();
            if (data.Length < 2 + ProtocolConstants.MaxMotdLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.ServerInfo)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Motd = reader.ReadFixedString(ProtocolConstants.MaxMotdLength);
            return true;
        }

        public static bool TryReadRaceResults(byte[] data, out PacketRaceResults packet)
        {
            packet = new PacketRaceResults();
            if (data.Length < 2 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.StopRace)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            var count = reader.ReadByte();
            var max = Math.Min(count, (byte)Math.Max(0, data.Length - 3));
            var results = new byte[max];
            for (var i = 0; i < max; i++)
                results[i] = reader.ReadByte();
            packet.Results = results;
            packet.NPlayers = max;
            return true;
        }

        public static bool TryReadPlayerJoined(byte[] data, out PacketPlayerJoined packet)
        {
            packet = new PacketPlayerJoined();
            if (data.Length < 2 + 4 + 1 + ProtocolConstants.MaxPlayerNameLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.PlayerJoined)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
            return true;
        }

        public static bool TryReadRoomList(byte[] data, out PacketRoomList packet)
        {
            packet = new PacketRoomList();
            if (data.Length < 2 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomList)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            var count = reader.ReadByte();
            var stride = 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12;
            var available = (data.Length - 3) / stride;
            var actualCount = Math.Min(count, available);
            var rooms = new PacketRoomSummary[actualCount];
            for (var i = 0; i < actualCount; i++)
            {
                rooms[i] = new PacketRoomSummary
                {
                    RoomId = reader.ReadUInt32(),
                    RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength),
                    RoomType = (GameRoomType)reader.ReadByte(),
                    PlayerCount = reader.ReadByte(),
                    PlayersToStart = reader.ReadByte(),
                    RaceStarted = reader.ReadBool(),
                    TrackName = reader.ReadFixedString(12)
                };
            }
            packet.Rooms = rooms;
            return true;
        }

        public static bool TryReadRoomState(byte[] data, out PacketRoomState packet)
        {
            packet = new PacketRoomState();
            if (data.Length < 2 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 12 + 1 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomState)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            packet.HostPlayerId = reader.ReadUInt32();
            packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
            packet.RoomType = (GameRoomType)reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            packet.InRoom = reader.ReadBool();
            packet.IsHost = reader.ReadBool();
            packet.RaceStarted = reader.ReadBool();
            packet.TrackName = reader.ReadFixedString(12);
            packet.Laps = reader.ReadByte();
            var count = reader.ReadByte();
            var stride = 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength;
            var available = Math.Max(0, (data.Length - (2 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 12 + 1 + 1)) / stride);
            var actualCount = Math.Min(count, available);
            var players = new PacketRoomPlayer[actualCount];
            for (var i = 0; i < actualCount; i++)
            {
                players[i] = new PacketRoomPlayer
                {
                    PlayerId = reader.ReadUInt32(),
                    PlayerNumber = reader.ReadByte(),
                    State = (PlayerState)reader.ReadByte(),
                    Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength)
                };
            }
            packet.Players = players;
            return true;
        }

        public static bool TryReadProtocolMessage(byte[] data, out PacketProtocolMessage packet)
        {
            packet = new PacketProtocolMessage();
            if (data.Length < 2 + 1 + ProtocolConstants.MaxProtocolMessageLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.ProtocolMessage)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Code = (ProtocolMessageCode)reader.ReadByte();
            packet.Message = reader.ReadFixedString(ProtocolConstants.MaxProtocolMessageLength);
            return true;
        }

        public static byte[] WritePlayerDataToServer(
            uint playerId,
            byte playerNumber,
            CarType car,
            PlayerRaceData raceData,
            PlayerState state,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring)
        {
            var buffer = WritePacketHeader(Command.PlayerDataToServer, 4 + 1 + 1 + 4 + 4 + 2 + 4 + 1 + 1 + 1 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerDataToServer);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteByte((byte)car);
            writer.WriteSingle(raceData.PositionX);
            writer.WriteSingle(raceData.PositionY);
            writer.WriteUInt16(raceData.Speed);
            writer.WriteInt32(raceData.Frequency);
            writer.WriteByte((byte)state);
            writer.WriteBool(engineRunning);
            writer.WriteBool(braking);
            writer.WriteBool(horning);
            writer.WriteBool(backfiring);
            return buffer;
        }

        public static byte[] WritePlayerState(Command command, uint playerId, byte playerNumber, PlayerState state)
        {
            var buffer = WritePacketHeader(command, 4 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteByte((byte)state);
            return buffer;
        }

        public static byte[] WritePlayer(Command command, uint playerId, byte playerNumber)
        {
            var buffer = WritePacketHeader(command, 4 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            return buffer;
        }

        public static byte[] WriteGeneral(Command command)
        {
            return WritePacketHeader(command, 0);
        }

        public static byte[] WriteRoomListRequest()
        {
            return WriteGeneral(Command.RoomListRequest);
        }

        public static byte[] WriteRoomCreate(string roomName, GameRoomType roomType, byte playersToStart)
        {
            var buffer = WritePacketHeader(Command.RoomCreate, ProtocolConstants.MaxRoomNameLength + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomCreate);
            writer.WriteFixedString(roomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)roomType);
            writer.WriteByte(playersToStart);
            return buffer;
        }

        public static byte[] WriteRoomJoin(uint roomId)
        {
            var buffer = WritePacketHeader(Command.RoomJoin, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomJoin);
            writer.WriteUInt32(roomId);
            return buffer;
        }

        public static byte[] WriteRoomLeave()
        {
            return WriteGeneral(Command.RoomLeave);
        }

        public static byte[] WriteRoomSetTrack(string trackName)
        {
            var buffer = WritePacketHeader(Command.RoomSetTrack, 12);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetTrack);
            writer.WriteFixedString(trackName ?? string.Empty, 12);
            return buffer;
        }

        public static byte[] WriteRoomSetLaps(byte laps)
        {
            var buffer = WritePacketHeader(Command.RoomSetLaps, 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetLaps);
            writer.WriteByte(laps);
            return buffer;
        }

        public static byte[] WriteRoomStartRace()
        {
            return WriteGeneral(Command.RoomStartRace);
        }

        public static byte[] WriteRoomSetPlayersToStart(byte playersToStart)
        {
            var buffer = WritePacketHeader(Command.RoomSetPlayersToStart, 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetPlayersToStart);
            writer.WriteByte(playersToStart);
            return buffer;
        }

        private static byte[] WritePacketHeader(Command command, int payloadSize)
        {
            var buffer = new byte[2 + payloadSize];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)command;
            return buffer;
        }
    }
}
