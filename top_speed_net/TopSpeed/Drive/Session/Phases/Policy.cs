using System;
using System.Collections.Generic;

namespace TopSpeed.Drive.Session
{
    internal sealed class Policy
    {
        private readonly Dictionary<Phase, PhaseDefinition> _phases;

        public Policy(
            Phase initialPhase,
            Phase resumeFallbackPhase,
            IEnumerable<PhaseDefinition> phases)
        {
            InitialPhase = initialPhase;
            ResumeFallbackPhase = resumeFallbackPhase;
            _phases = new Dictionary<Phase, PhaseDefinition>();

            foreach (var phase in phases ?? throw new ArgumentNullException(nameof(phases)))
                _phases[phase.Phase] = phase;

            if (!_phases.ContainsKey(initialPhase))
                throw new ArgumentException("Initial phase is not defined.", nameof(initialPhase));
            if (!_phases.ContainsKey(resumeFallbackPhase))
                throw new ArgumentException("Resume fallback phase is not defined.", nameof(resumeFallbackPhase));
        }

        public Phase InitialPhase { get; }
        public Phase ResumeFallbackPhase { get; }

        public PhaseDefinition GetPhase(Phase phase)
        {
            if (!_phases.TryGetValue(phase, out var definition))
                throw new InvalidOperationException($"Phase '{phase}' is not configured.");
            return definition;
        }
    }
}

