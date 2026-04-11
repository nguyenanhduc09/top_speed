using System;
using System.Collections.Generic;

namespace TopSpeed.Drive.Session
{
    internal sealed class PolicyBuilder
    {
        private readonly Phase _initialPhase;
        private readonly Phase _resumeFallbackPhase;
        private readonly List<PhaseDefinition> _definitions;

        public PolicyBuilder(Phase initialPhase, Phase resumeFallbackPhase)
        {
            _initialPhase = initialPhase;
            _resumeFallbackPhase = resumeFallbackPhase;
            _definitions = new List<PhaseDefinition>();
        }

        public PolicyBuilder Add(
            Phase phase,
            bool advanceProgressClock,
            bool advanceRuntimeClock,
            InputPolicy inputPolicy,
            IReadOnlyCollection<SubsystemId> activeSubsystems,
            IReadOnlyCollection<CommandId> allowedCommands,
            IReadOnlyCollection<ExternalEventId> allowedExternalEvents,
            IReadOnlyCollection<Phase> allowedTransitions)
        {
            _definitions.Add(new PhaseDefinition(
                phase,
                advanceProgressClock,
                advanceRuntimeClock,
                inputPolicy,
                activeSubsystems,
                allowedCommands,
                allowedExternalEvents,
                allowedTransitions));
            return this;
        }

        public Policy Build()
        {
            return new Policy(_initialPhase, _resumeFallbackPhase, _definitions);
        }
    }
}
