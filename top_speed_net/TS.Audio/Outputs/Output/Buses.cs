using System;
using System.Collections.Generic;

namespace TS.Audio
{
    public sealed partial class AudioOutput
    {
        public AudioBus CreateBus(string name)
        {
            return CreateBus(name, _mainBus);
        }

        public AudioBus CreateBus(string name, AudioBus? parent)
        {
            return CreateBus(name, parent, null);
        }

        internal AudioBus CreateBus(string name, AudioBus? parent, PlaybackPolicy? defaults)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Bus name is required.", nameof(name));

            lock (_busLock)
            {
                if (_buses.TryGetValue(name, out var existing))
                    return existing;

                return CreateBusInternal(name, parent ?? _mainBus, defaults);
            }
        }

        public AudioBus GetBus(string name)
        {
            lock (_busLock)
            {
                if (_buses.TryGetValue(name, out var bus))
                    return bus;
            }

            throw new KeyNotFoundException("Audio bus not found: " + name);
        }

        private AudioBus CreateBusInternal(string name, AudioBus? parent, PlaybackPolicy? defaults = null)
        {
            var bus = new AudioBus(this, name, parent, defaults);
            _buses[name] = bus;
            return bus;
        }
    }
}
