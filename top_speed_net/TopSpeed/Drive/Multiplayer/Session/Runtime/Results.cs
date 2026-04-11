using System;
using System.Collections.Generic;
using TopSpeed.Drive;
using TopSpeed.Drive.Session;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private DriveResultSummary BuildResultSummary(PacketRoomRaceCompleted packet)
        {
            var source = packet?.Results ?? Array.Empty<PacketRoomRaceResultEntry>();
            var entries = new List<DriveResultEntry>(source.Length > 0 ? source.Length : 1);
            var localPlayerNumber = LocalPlayerNumber;
            var localPosition = 0;

            for (var i = 0; i < source.Length; i++)
            {
                var result = source[i];
                var position = i + 1;
                var isLocal = result.PlayerNumber == localPlayerNumber;
                if (isLocal)
                    localPosition = position;

                var name = _resolvePlayerName(result.PlayerNumber);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = LocalizationService.Format(
                        LocalizationService.Mark("Player {0}"),
                        result.PlayerNumber + 1);
                }

                entries.Add(new DriveResultEntry
                {
                    Name = name,
                    Position = position,
                    TimeMs = result.Status == RoomRaceResultStatus.Finished ? Math.Max(0, result.TimeMs) : 0,
                    IsLocalPlayer = isLocal
                });
            }

            if (localPosition == 0)
            {
                localPosition = Math.Max(1, entries.Count + 1);
                entries.Add(new DriveResultEntry
                {
                    Name = _resolvePlayerName(localPlayerNumber),
                    Position = localPosition,
                    TimeMs = Math.Max(0, _raceTime),
                    IsLocalPlayer = true
                });
            }

            return new DriveResultSummary
            {
                IsMultiplayer = true,
                LocalPosition = localPosition,
                LocalCrashCount = _localCrashCount,
                Entries = entries.ToArray()
            };
        }

        private bool UpdateExitWhenQueueIdle()
        {
            if (!_exitWhenQueueIdle)
                return false;
            if (_requirePostFinishStopBeforeExit && !AreVehiclesSettledForExit())
                return false;
            if (!_soundQueue.IsIdle)
                return false;
            if (_session.Context.Phase == Phase.Finishing)
                _session.SetPhase(Phase.Finished);
            return true;
        }

        private bool AreVehiclesSettledForExit()
        {
            if (_serverStopReceived)
                return true;
            if (_car.Speed > PostFinishStopSpeedKph)
                return false;

            foreach (var remote in _remotePlayers.Values)
            {
                if (!remote.Finished)
                    continue;
                if (remote.Player.Speed > RemoteSettledSpeedKph)
                    return false;
            }

            return true;
        }

        private void FinalizeServerRace(DriveResultSummary? summary)
        {
            if (_serverStopReceived)
                return;

            _serverStopReceived = true;
            _snapshotFrames.Clear();
            _hasSnapshotTickNow = false;
            _missingSnapshotPlayers.Clear();
            foreach (var number in _remotePlayers.Keys)
                _missingSnapshotPlayers.Add(number);
            for (var i = 0; i < _missingSnapshotPlayers.Count; i++)
                RemoveRemotePlayer(_missingSnapshotPlayers[i]);
            _remoteLiveStates.Clear();
            if (!_sentFinish)
            {
                _sentFinish = true;
                _currentState = PlayerState.Finished;
                SendPlayerState(sendStarted: false);
            }

            if (summary != null)
                _pendingResultSummary = summary;

            _session.SetPhase(Phase.Finishing);
            _exitWhenQueueIdle = true;
        }
    }
}
