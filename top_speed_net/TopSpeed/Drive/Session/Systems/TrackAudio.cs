using System;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class TrackAudio
    {
        private readonly DriveSettings _settings;
        private readonly Source?[][] _randomSounds;
        private readonly int[] _totalRandomSounds;
        private readonly Source? _turnEndDing;
        private readonly Action<Source?> _queueSound;
        private readonly Action<Event, float> _queueEvent;
        private TrackType _lastRoadTypeAtPosition;
        private bool _hasLastRoadTypeAtPosition;

        public TrackAudio(
            DriveSettings settings,
            Source?[][] randomSounds,
            int[] totalRandomSounds,
            Source? turnEndDing,
            Action<Source?> queueSound,
            Action<Event, float> queueEvent)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _randomSounds = randomSounds ?? throw new ArgumentNullException(nameof(randomSounds));
            _totalRandomSounds = totalRandomSounds ?? throw new ArgumentNullException(nameof(totalRandomSounds));
            _turnEndDing = turnEndDing;
            _queueSound = queueSound ?? throw new ArgumentNullException(nameof(queueSound));
            _queueEvent = queueEvent ?? throw new ArgumentNullException(nameof(queueEvent));
            Reset();
        }

        public void Reset()
        {
            _lastRoadTypeAtPosition = TrackType.Straight;
            _hasLastRoadTypeAtPosition = false;
        }

        public void HandleRoad(Track.Road road)
        {
            var currentType = road.Type;
            if (_hasLastRoadTypeAtPosition &&
                _lastRoadTypeAtPosition != TrackType.Straight &&
                currentType == TrackType.Straight &&
                _turnEndDing != null)
            {
                _turnEndDing.Stop();
                _turnEndDing.SeekToStart();
                _turnEndDing.Play(loop: false);
            }

            _lastRoadTypeAtPosition = currentType;
            _hasLastRoadTypeAtPosition = true;
        }

        public Track.Road AnnounceNextRoad(Track.Road currentRoad, Track.Road nextRoad)
        {
            if ((int)_settings.Copilot > 0 && nextRoad.Type != TrackType.Straight)
            {
                var index = (int)nextRoad.Type - 1;
                if (index >= 0 && index < _randomSounds.Length && index < _totalRandomSounds.Length && _totalRandomSounds[index] > 0)
                {
                    var sound = _randomSounds[index][Algorithm.RandomInt(_totalRandomSounds[index])];
                    _queueSound(sound);
                }
            }

            if ((int)_settings.Copilot > 1 && nextRoad.Surface != currentRoad.Surface)
            {
                var index = (int)nextRoad.Surface + 8;
                if (index >= 0 && index < _randomSounds.Length && index < _totalRandomSounds.Length && _totalRandomSounds[index] > 0)
                {
                    var sound = _randomSounds[index][Algorithm.RandomInt(_totalRandomSounds[index])];
                    _queueEvent(new Event(Events.PlaySound, sound), 1.0f);
                }
            }

            return nextRoad;
        }
    }
}
