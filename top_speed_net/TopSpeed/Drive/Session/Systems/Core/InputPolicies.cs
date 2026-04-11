using System;

namespace TopSpeed.Drive.Session
{
    internal sealed class InputPolicies
    {
        public void Apply(SessionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.InputPolicy = context.PhaseDefinition.InputPolicy;
        }
    }
}

