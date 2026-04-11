using System;
using System.Collections.Generic;

namespace TopSpeed.Drive.Session
{
    internal sealed class SessionContext
    {
        private readonly Policy _policy;
        private readonly Dictionary<Type, object> _state;

        internal SessionContext(Policy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _state = new Dictionary<Type, object>();
            Reset();
        }

        public Phase Phase { get; internal set; }
        public PhaseDefinition PhaseDefinition => _policy.GetPhase(Phase);
        public InputPolicy InputPolicy { get; internal set; } = InputPolicy.Create(false, false, false);
        public float ProgressSeconds { get; internal set; }
        public float RuntimeSeconds { get; internal set; }
        public int ProgressMilliseconds => (int)(ProgressSeconds * 1000f);
        public bool WantsPause { get; internal set; }
        public bool WantsExit { get; internal set; }

        public void SetState<T>(T value) where T : class
        {
            _state[typeof(T)] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public T GetState<T>() where T : class
        {
            if (_state.TryGetValue(typeof(T), out var value))
                return (T)value;

            throw new InvalidOperationException($"Session state '{typeof(T).Name}' is not registered.");
        }

        public bool TryGetState<T>(out T? value) where T : class
        {
            if (_state.TryGetValue(typeof(T), out var raw))
            {
                value = (T)raw;
                return true;
            }

            value = null;
            return false;
        }

        internal void Reset()
        {
            Phase = _policy.InitialPhase;
            InputPolicy = _policy.GetPhase(Phase).InputPolicy;
            ProgressSeconds = 0f;
            RuntimeSeconds = 0f;
            WantsPause = false;
            WantsExit = false;
        }
    }
}

