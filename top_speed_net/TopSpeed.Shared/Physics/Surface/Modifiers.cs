namespace TopSpeed.Physics.Surface
{
    public readonly struct SurfaceModifiers
    {
        public SurfaceModifiers(float traction, float deceleration, float lateralSpeedMultiplier)
        {
            Traction = traction;
            Deceleration = deceleration;
            LateralSpeedMultiplier = lateralSpeedMultiplier;
        }

        public float Traction { get; }
        public float Deceleration { get; }
        public float LateralSpeedMultiplier { get; }
    }
}
