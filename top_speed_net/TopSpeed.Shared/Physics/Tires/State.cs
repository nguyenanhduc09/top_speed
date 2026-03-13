namespace TopSpeed.Physics.Tires
{
    public readonly struct TireModelState
    {
        public TireModelState(float lateralVelocityMps, float yawRateRad)
        {
            LateralVelocityMps = lateralVelocityMps;
            YawRateRad = yawRateRad;
        }

        public float LateralVelocityMps { get; }
        public float YawRateRad { get; }
    }
}
