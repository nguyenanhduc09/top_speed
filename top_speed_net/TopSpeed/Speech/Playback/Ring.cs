using System;

namespace TopSpeed.Speech.Playback
{
    internal sealed class Ring
    {
        private float[] _buffer;
        private int _start;
        private int _count;

        public Ring(int capacity = 16384)
        {
            _buffer = new float[Math.Max(1, capacity)];
        }

        public int Count => _count;

        public void Clear()
        {
            _start = 0;
            _count = 0;
        }

        public void Write(float[] samples, int offset, int count)
        {
            if (samples == null)
                throw new ArgumentNullException(nameof(samples));

            if (count <= 0)
                return;

            EnsureCapacity(_count + count);

            var end = (_start + _count) % _buffer.Length;
            var first = Math.Min(count, _buffer.Length - end);
            Array.Copy(samples, offset, _buffer, end, first);

            var remaining = count - first;
            if (remaining > 0)
                Array.Copy(samples, offset + first, _buffer, 0, remaining);

            _count += count;
        }

        public int Read(float[] destination, int offset, int count)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (count <= 0 || _count == 0)
                return 0;

            var actual = Math.Min(count, _count);
            var first = Math.Min(actual, _buffer.Length - _start);
            Array.Copy(_buffer, _start, destination, offset, first);

            var remaining = actual - first;
            if (remaining > 0)
                Array.Copy(_buffer, 0, destination, offset + first, remaining);

            _start = (_start + actual) % _buffer.Length;
            _count -= actual;
            if (_count == 0)
                _start = 0;

            return actual;
        }

        private void EnsureCapacity(int required)
        {
            if (_buffer.Length >= required)
                return;

            var expanded = new float[Math.Max(required, _buffer.Length * 2)];
            if (_count > 0)
            {
                var first = Math.Min(_count, _buffer.Length - _start);
                Array.Copy(_buffer, _start, expanded, 0, first);

                var remaining = _count - first;
                if (remaining > 0)
                    Array.Copy(_buffer, 0, expanded, first, remaining);
            }

            _buffer = expanded;
            _start = 0;
        }
    }
}
