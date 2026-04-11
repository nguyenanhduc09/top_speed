using System;

namespace TopSpeed.Drive.Session
{
    internal readonly struct ExternalEventId : IEquatable<ExternalEventId>
    {
        private readonly string _value;

        public ExternalEventId(string value)
        {
            _value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("External event id is required.", nameof(value))
                : value;
        }

        public bool Equals(ExternalEventId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is ExternalEventId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value;
        }

        public static bool operator ==(ExternalEventId left, ExternalEventId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ExternalEventId left, ExternalEventId right)
        {
            return !left.Equals(right);
        }
    }
}
