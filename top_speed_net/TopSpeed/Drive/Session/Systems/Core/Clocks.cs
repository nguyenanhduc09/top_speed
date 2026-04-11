namespace TopSpeed.Drive.Session
{
    internal sealed class Clocks
    {
        public void Advance(SessionContext context, float elapsed)
        {
            if (elapsed <= 0f || context.WantsExit)
                return;

            var definition = context.PhaseDefinition;
            if (definition.AdvanceProgressClock)
                context.ProgressSeconds += elapsed;
            if (definition.AdvanceRuntimeClock)
                context.RuntimeSeconds += elapsed;
        }
    }
}

