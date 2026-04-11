using System;

namespace TopSpeed.Drive.Session
{
    internal readonly struct SubsystemId : IEquatable<SubsystemId>
    {
        private readonly string _value;

        public SubsystemId(string value)
        {
            _value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Subsystem id is required.", nameof(value))
                : value;
        }

        public bool Equals(SubsystemId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is SubsystemId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value;
        }

        public static bool operator ==(SubsystemId left, SubsystemId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SubsystemId left, SubsystemId right)
        {
            return !left.Equals(right);
        }
    }
}
