namespace TopSpeed.Physics.Tires
{
    public static class TireModelSolver
    {
        public static TireModelOutput Solve(in TireModelParameters parameters, in TireModelInput input, in TireModelState state)
        {
            var steer = TireSteer.Resolve(parameters, input);
            var grip = TireGrip.Resolve(parameters, input, steer);
            var axle = TireAxle.Compute(parameters, state, steer, grip);
            return TireStep.Solve(parameters, input, state, steer, axle);
        }
    }
}
