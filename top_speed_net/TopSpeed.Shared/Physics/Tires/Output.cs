namespace TopSpeed.Physics.Tires
{
    public readonly struct TireModelOutput
    {
        public TireModelOutput(float longitudinalGripFactor, float lateralSpeedMps, TireModelState state)
        {
            LongitudinalGripFactor = longitudinalGripFactor;
            LateralSpeedMps = lateralSpeedMps;
            State = state;
        }

        public float LongitudinalGripFactor { get; }
        public float LateralSpeedMps { get; }
        public TireModelState State { get; }
    }
}
