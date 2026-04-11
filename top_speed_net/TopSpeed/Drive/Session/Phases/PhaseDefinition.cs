using System;
using System.Collections.Generic;
using System.Linq;

namespace TopSpeed.Drive.Session
{
    internal sealed class PhaseDefinition
    {
        public PhaseDefinition(
            Phase phase,
            bool advanceProgressClock,
            bool advanceRuntimeClock,
            InputPolicy inputPolicy,
            IReadOnlyCollection<SubsystemId> activeSubsystems,
            IReadOnlyCollection<CommandId> allowedCommands,
            IReadOnlyCollection<ExternalEventId> allowedExternalEvents,
            IReadOnlyCollection<Phase> allowedTransitions)
        {
            Phase = phase;
            AdvanceProgressClock = advanceProgressClock;
            AdvanceRuntimeClock = advanceRuntimeClock;
            InputPolicy = inputPolicy ?? throw new ArgumentNullException(nameof(inputPolicy));
            ActiveSubsystems = activeSubsystems ?? throw new ArgumentNullException(nameof(activeSubsystems));
            AllowedCommands = allowedCommands ?? throw new ArgumentNullException(nameof(allowedCommands));
            AllowedExternalEvents = allowedExternalEvents ?? throw new ArgumentNullException(nameof(allowedExternalEvents));
            AllowedTransitions = allowedTransitions ?? throw new ArgumentNullException(nameof(allowedTransitions));
        }

        public Phase Phase { get; }
        public bool AdvanceProgressClock { get; }
        public bool AdvanceRuntimeClock { get; }
        public InputPolicy InputPolicy { get; }
        public IReadOnlyCollection<SubsystemId> ActiveSubsystems { get; }
        public IReadOnlyCollection<CommandId> AllowedCommands { get; }
        public IReadOnlyCollection<ExternalEventId> AllowedExternalEvents { get; }
        public IReadOnlyCollection<Phase> AllowedTransitions { get; }

        public static IReadOnlyCollection<SubsystemId> Subsystems(params Subsystem[] subsystems)
        {
            if (subsystems == null)
                throw new ArgumentNullException(nameof(subsystems));

            return subsystems
                .Where(subsystem => subsystem != null)
                .Select(subsystem => subsystem.Id)
                .ToArray();
        }
    }
}
