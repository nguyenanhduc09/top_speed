namespace TopSpeed.Physics.Tires
{
    public readonly struct TireModelInput
    {
        public TireModelInput(float elapsedSeconds, float speedMps, int steeringInput, float surfaceTractionMod, float surfaceLateralMultiplier)
        {
            ElapsedSeconds = elapsedSeconds;
            SpeedMps = speedMps;
            SteeringInput = steeringInput;
            SurfaceTractionMod = surfaceTractionMod;
            SurfaceLateralMultiplier = surfaceLateralMultiplier;
        }

        public float ElapsedSeconds { get; }
        public float SpeedMps { get; }
        public int SteeringInput { get; }
        public float SurfaceTractionMod { get; }
        public float SurfaceLateralMultiplier { get; }
    }
}
