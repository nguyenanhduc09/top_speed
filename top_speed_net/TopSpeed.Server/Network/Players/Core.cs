using TopSpeed.Protocol;
using TopSpeed.Localization;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void HandlePlayerHello(PlayerConnection player, PacketPlayerHello hello)
        {
            var name = (hello.Name ?? string.Empty).Trim();
            if (name.Length > ProtocolConstants.MaxPlayerNameLength)
                name = name.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            player.Name = name;
            if (!player.ServerPresenceAnnounced)
            {
                player.ServerPresenceAnnounced = true;
                BroadcastServerConnectAnnouncement(player);
            }
            if (player.RoomId.HasValue && _rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                TouchRoomVersion(room);
                EmitRoomParticipantEvent(
                    room,
                    RoomEventKind.ParticipantStateChanged,
                    player.Id,
                    player.PlayerNumber,
                    player.State,
                    string.IsNullOrWhiteSpace(player.Name)
                        ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                        : player.Name);
            }
        }

        private void HandlePlayerState(PlayerConnection player, PacketPlayerState state)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerState++;
                return;
            }

            var previousState = player.State;

            if (room.RaceStarted)
            {
                if (state.State == PlayerState.AwaitingStart
                    || state.State == PlayerState.Racing
                    || state.State == PlayerState.Finished)
                {
                    player.State = state.State;
                }
                else
                {
                    _authorityDropsPlayerState++;
                }
            }
            else
            {
                if (state.State != PlayerState.NotReady && state.State != PlayerState.Undefined)
                    _authorityDropsPlayerState++;
                player.State = PlayerState.NotReady;
                if (room.TrackSelected)
                    SendTrack(room, player);
            }

            if (previousState != player.State)
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Player state transition: room={0}, player={1}, {2} -> {3} (packet={4})."),
                    room.Id,
                    player.Id,
                    previousState,
                    player.State,
                    state.State));
            if (previousState != player.State)
            {
                TouchRoomVersion(room);
                EmitRoomParticipantEvent(
                    room,
                    RoomEventKind.ParticipantStateChanged,
                    player.Id,
                    player.PlayerNumber,
                    player.State,
                    string.IsNullOrWhiteSpace(player.Name)
                        ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                        : player.Name);
            }
        }

        private void HandlePlayerData(PlayerConnection player, PacketPlayerData data)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerData++;
                return;
            }

            var previousState = player.State;
            player.Car = NormalizeNetworkCar(data.Car);
            ApplyVehicleDimensions(player, player.Car);
            player.PositionX = data.RaceData.PositionX;
            player.PositionY = data.RaceData.PositionY;
            player.Speed = data.RaceData.Speed;
            player.Frequency = data.RaceData.Frequency;
            player.EngineRunning = data.EngineRunning;
            player.Braking = data.Braking;
            player.Horning = data.Horning;
            player.Backfiring = data.Backfiring;
            UpdateMediaState(player, room, data);
            var nextState = data.State;

            if (room.RaceStarted)
            {
                if (nextState == PlayerState.Undefined || nextState == PlayerState.NotReady)
                {
                    _authorityDropsPlayerData++;
                    nextState = player.State;
                }

                if (nextState != PlayerState.AwaitingStart
                    && nextState != PlayerState.Racing
                    && nextState != PlayerState.Finished)
                {
                    _authorityDropsPlayerData++;
                    nextState = player.State;
                }
            }
            else
            {
                if (nextState != PlayerState.NotReady && nextState != PlayerState.Undefined)
                    _authorityDropsPlayerData++;
                nextState = PlayerState.NotReady;
            }

            player.State = nextState;
            if (previousState != nextState)
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Player state transition from data: room={0}, player={1}, {2} -> {3}."),
                    room.Id,
                    player.Id,
                    previousState,
                    nextState));
        }

        private void HandlePlayerStarted(PlayerConnection player)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerStarted++;
                return;
            }
            if (!room.RaceStarted)
            {
                _authorityDropsPlayerStarted++;
                return;
            }

            if (player.State == PlayerState.AwaitingStart || player.State == PlayerState.Racing)
            {
                player.State = PlayerState.Racing;
            }
            else
            {
                _authorityDropsPlayerStarted++;
            }
        }

    }
}
