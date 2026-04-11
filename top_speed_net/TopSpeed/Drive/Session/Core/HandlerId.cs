using System;

namespace TopSpeed.Drive.Session
{
    internal readonly struct HandlerId : IEquatable<HandlerId>
    {
        private readonly string _value;

        public HandlerId(string value)
        {
            _value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Handler id is required.", nameof(value))
                : value;
        }

        public bool Equals(HandlerId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is HandlerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value;
        }

        public static bool operator ==(HandlerId left, HandlerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HandlerId left, HandlerId right)
        {
            return !left.Equals(right);
        }
    }
}
