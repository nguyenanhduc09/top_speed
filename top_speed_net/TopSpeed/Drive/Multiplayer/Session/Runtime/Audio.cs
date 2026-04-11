using System;
using TopSpeed.Drive.Session;
using TopSpeed.Localization;
using AudioSource = TS.Audio.Source;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void Speak(AudioSource sound, bool unKey = false)
        {
            if (sound == null)
                return;

            var length = Math.Max(0.05f, sound.LengthSeconds);
            _speakTime = Math.Max(_speakTime, _session.Context.ProgressSeconds) + length;
            QueueSound(sound);
            if (unKey)
            {
                _unkeyQueue++;
                _session.QueueEvent(new Event(Events.PlayUnkey), length);
            }
        }

        private void SpeakText(string text)
        {
            _speech.Speak(text);
        }

        private void QueueSound(AudioSource? sound)
        {
            if (sound != null)
                _soundQueue.Enqueue(sound);
        }

        private bool TrySendRace(bool sent)
        {
            if (sent)
                return true;
            if (_sendFailureAnnounced)
                return false;

            _sendFailureAnnounced = true;
            SpeakText(LocalizationService.Mark("Network send failed. Please check your connection."));
            return false;
        }
    }
}
