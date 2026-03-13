using System;

namespace TopSpeed.Physics.Tires
{
    internal static class TireModelMath
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        public static float Lerp(float from, float to, float t)
        {
            return from + ((to - from) * Clamp01(t));
        }

        public static float DegToRad(float degrees)
        {
            return (float)(Math.PI / 180.0) * degrees;
        }

        public static float Sign(float value)
        {
            if (value < 0f)
                return -1f;
            if (value > 0f)
                return 1f;
            return 0f;
        }

        public static float Pow(float value, float exponent)
        {
            return (float)Math.Pow(value, exponent);
        }

        public static float SafeDiv(float numerator, float denominator, float fallback)
        {
            return Math.Abs(denominator) > 0.00001f ? (numerator / denominator) : fallback;
        }
    }
}
