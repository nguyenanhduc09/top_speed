using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static class PacketSerializer
    {
        public static bool TryReadHeader(byte[] data, out PacketHeader header)
        {
            header = new PacketHeader();
            if (data.Length < 2)
                return false;
            header.Version = data[0];
            header.Command = (Command)data[1];
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

        public static bool TryReadPlayerHello(byte[] data, out PacketPlayerHello packet)
        {
            packet = new PacketPlayerHello();
            if (data.Length < 2 + ProtocolConstants.MaxPlayerNameLength)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
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

        public static bool TryReadRoomCreate(byte[] data, out PacketRoomCreate packet)
        {
            packet = new PacketRoomCreate();
            if (data.Length < 2 + ProtocolConstants.MaxRoomNameLength + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
            packet.RoomType = (GameRoomType)reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            return true;
        }

        public static bool TryReadRoomJoin(byte[] data, out PacketRoomJoin packet)
        {
            packet = new PacketRoomJoin();
            if (data.Length < 2 + 4)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            return true;
        }

        public static bool TryReadRoomSetTrack(byte[] data, out PacketRoomSetTrack packet)
        {
            packet = new PacketRoomSetTrack();
            if (data.Length < 2 + 12)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.TrackName = reader.ReadFixedString(12);
            return true;
        }

        public static bool TryReadRoomSetLaps(byte[] data, out PacketRoomSetLaps packet)
        {
            packet = new PacketRoomSetLaps();
            if (data.Length < 2 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Laps = reader.ReadByte();
            return true;
        }

        public static bool TryReadRoomSetPlayersToStart(byte[] data, out PacketRoomSetPlayersToStart packet)
        {
            packet = new PacketRoomSetPlayersToStart();
            if (data.Length < 2 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            return true;
        }

        public static byte[] WritePacketHeader(Command command, int payloadSize)
        {
            var buffer = new byte[2 + payloadSize];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)command;
            return buffer;
        }

        public static byte[] WritePlayerNumber(uint id, byte playerNumber)
        {
            return WritePlayer(Command.PlayerNumber, id, playerNumber);
        }

        public static byte[] WritePlayer(Command command, uint id, byte playerNumber)
        {
            var buffer = WritePacketHeader(command, 4 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(id);
            writer.WriteByte(playerNumber);
            return buffer;
        }

        public static byte[] WritePlayerState(Command command, uint id, byte playerNumber, PlayerState state)
        {
            var buffer = WritePacketHeader(command, 4 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(id);
            writer.WriteByte(playerNumber);
            writer.WriteByte((byte)state);
            return buffer;
        }

        public static byte[] WritePlayerData(PacketPlayerData data)
        {
            var buffer = WritePacketHeader(Command.PlayerData, 4 + 1 + 1 + 4 + 4 + 2 + 4 + 1 + 1 + 1 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerData);
            writer.WriteUInt32(data.PlayerId);
            writer.WriteByte(data.PlayerNumber);
            writer.WriteByte((byte)data.Car);
            writer.WriteSingle(data.RaceData.PositionX);
            writer.WriteSingle(data.RaceData.PositionY);
            writer.WriteUInt16(data.RaceData.Speed);
            writer.WriteInt32(data.RaceData.Frequency);
            writer.WriteByte((byte)data.State);
            writer.WriteBool(data.EngineRunning);
            writer.WriteBool(data.Braking);
            writer.WriteBool(data.Horning);
            writer.WriteBool(data.Backfiring);
            return buffer;
        }

        public static byte[] WritePlayerBumped(PacketPlayerBumped bump)
        {
            var buffer = WritePacketHeader(Command.PlayerBumped, 4 + 1 + 4 + 4 + 2);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerBumped);
            writer.WriteUInt32(bump.PlayerId);
            writer.WriteByte(bump.PlayerNumber);
            writer.WriteSingle(bump.BumpX);
            writer.WriteSingle(bump.BumpY);
            writer.WriteUInt16(bump.BumpSpeed);
            return buffer;
        }

        public static byte[] WriteLoadCustomTrack(PacketLoadCustomTrack track)
        {
            var maxLength = Math.Min(track.TrackLength, (ushort)ProtocolConstants.MaxMultiTrackLength);
            var definitionCount = Math.Min(track.Definitions.Length, maxLength);
            var payload = 1 + 12 + 1 + 1 + 2 + (definitionCount * (1 + 1 + 1 + 4));
            var buffer = WritePacketHeader(Command.LoadCustomTrack, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.LoadCustomTrack);
            writer.WriteByte(track.NrOfLaps);
            writer.WriteFixedString(track.TrackName, 12);
            writer.WriteByte((byte)track.TrackWeather);
            writer.WriteByte((byte)track.TrackAmbience);
            writer.WriteUInt16(maxLength);
            for (var i = 0; i < definitionCount; i++)
            {
                var def = track.Definitions[i];
                writer.WriteByte((byte)def.Type);
                writer.WriteByte((byte)def.Surface);
                writer.WriteByte((byte)def.Noise);
                writer.WriteSingle(def.Length);
            }
            return buffer;
        }

        public static byte[] WriteRaceResults(PacketRaceResults results)
        {
            var count = Math.Min(results.Results.Length, ProtocolConstants.MaxPlayers);
            var payload = 1 + count;
            var buffer = WritePacketHeader(Command.StopRace, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.StopRace);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
                writer.WriteByte(results.Results[i]);
            return buffer;
        }

        public static byte[] WriteServerInfo(PacketServerInfo info)
        {
            var buffer = WritePacketHeader(Command.ServerInfo, ProtocolConstants.MaxMotdLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ServerInfo);
            writer.WriteFixedString(info.Motd ?? string.Empty, ProtocolConstants.MaxMotdLength);
            return buffer;
        }

        public static byte[] WritePlayerJoined(PacketPlayerJoined joined)
        {
            var buffer = WritePacketHeader(Command.PlayerJoined, 4 + 1 + ProtocolConstants.MaxPlayerNameLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerJoined);
            writer.WriteUInt32(joined.PlayerId);
            writer.WriteByte(joined.PlayerNumber);
            writer.WriteFixedString(joined.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public static byte[] WriteRoomList(PacketRoomList list)
        {
            var count = Math.Min(list.Rooms.Length, ProtocolConstants.MaxRoomListEntries);
            var payload = 1 + (count * (4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12));
            var buffer = WritePacketHeader(Command.RoomList, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomList);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var room = list.Rooms[i];
                writer.WriteUInt32(room.RoomId);
                writer.WriteFixedString(room.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
                writer.WriteByte((byte)room.RoomType);
                writer.WriteByte(room.PlayerCount);
                writer.WriteByte(room.PlayersToStart);
                writer.WriteBool(room.RaceStarted);
                writer.WriteFixedString(room.TrackName ?? string.Empty, 12);
            }
            return buffer;
        }

        public static byte[] WriteRoomState(PacketRoomState state)
        {
            var count = Math.Min(state.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 12 + 1 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength));
            var buffer = WritePacketHeader(Command.RoomState, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomState);
            writer.WriteUInt32(state.RoomId);
            writer.WriteUInt32(state.HostPlayerId);
            writer.WriteFixedString(state.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)state.RoomType);
            writer.WriteByte(state.PlayersToStart);
            writer.WriteBool(state.InRoom);
            writer.WriteBool(state.IsHost);
            writer.WriteBool(state.RaceStarted);
            writer.WriteFixedString(state.TrackName ?? string.Empty, 12);
            writer.WriteByte(state.Laps);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var player = state.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.State);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            }
            return buffer;
        }

        public static byte[] WriteProtocolMessage(PacketProtocolMessage message)
        {
            var buffer = WritePacketHeader(Command.ProtocolMessage, 1 + ProtocolConstants.MaxProtocolMessageLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ProtocolMessage);
            writer.WriteByte((byte)message.Code);
            writer.WriteFixedString(message.Message ?? string.Empty, ProtocolConstants.MaxProtocolMessageLength);
            return buffer;
        }

        public static byte[] WriteGeneral(Command command)
        {
            var buffer = WritePacketHeader(command, 0);
            return buffer;
        }
    }
}
