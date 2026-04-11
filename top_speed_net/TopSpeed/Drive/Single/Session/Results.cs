using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Drive;

namespace TopSpeed.Drive.Single
{
    internal sealed partial class SingleSession
    {
        private int ReadCurrentRaceTimeMs()
        {
            return Math.Max(0, _session.Context.ProgressMilliseconds);
        }

        private void RecordFinish(int playerNumber, int timeMs)
        {
            if (!_finishOrder.Contains(playerNumber))
                _finishOrder.Add(playerNumber);
            _finishTimesMs[playerNumber] = Math.Max(0, timeMs);
        }

        private void EnsureAllFinishEntries()
        {
            for (var playerNumber = 0; playerNumber <= _nComputerPlayers; playerNumber++)
            {
                if (_finishTimesMs.ContainsKey(playerNumber))
                    continue;

                _finishTimesMs[playerNumber] = ReadCurrentRaceTimeMs();
                if (!_finishOrder.Contains(playerNumber))
                    _finishOrder.Add(playerNumber);
            }
        }

        private DriveResultSummary BuildResultSummary()
        {
            EnsureAllFinishEntries();

            var entries = new List<DriveResultEntry>(_finishOrder.Count);
            var localPosition = 1;
            for (var i = 0; i < _finishOrder.Count; i++)
            {
                var playerNumber = _finishOrder[i];
                var position = i + 1;
                if (playerNumber == _playerNumber)
                    localPosition = position;

                entries.Add(new DriveResultEntry
                {
                    Name = LocalizationService.Format(LocalizationService.Mark("player {0}"), playerNumber + 1),
                    Position = position,
                    TimeMs = _finishTimesMs.TryGetValue(playerNumber, out var timeMs) ? timeMs : 0,
                    IsLocalPlayer = playerNumber == _playerNumber
                });
            }

            return new DriveResultSummary
            {
                IsMultiplayer = false,
                LocalPosition = localPosition,
                LocalCrashCount = _localCrashCount,
                Entries = entries.ToArray()
            };
        }

        private bool CheckFinish()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                if (_computerPlayers[i]?.Finished == false)
                    return false;
            }

            return _lap > _nrOfLaps;
        }

        private bool AreVehiclesSettledForExit()
        {
            if (_car.Speed > PostFinishStopSpeedKph)
                return false;

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null || !bot.Finished)
                    continue;
                if (bot.Speed > BotSettledSpeedKph)
                    return false;
            }

            return true;
        }
    }
}


