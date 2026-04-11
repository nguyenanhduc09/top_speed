using System;
using System.Linq;

namespace TopSpeed.Drive.Session
{
    internal sealed class Phases
    {
        private readonly Policy _policy;
        private Phase _resumePhase;

        public Phases(Policy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _resumePhase = policy.ResumeFallbackPhase;
        }

        public void Reset(SessionContext context, InputPolicies inputPolicies)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (inputPolicies == null)
                throw new ArgumentNullException(nameof(inputPolicies));

            _resumePhase = _policy.ResumeFallbackPhase;
            inputPolicies.Apply(context);
        }

        public void SetPhase(
            SessionContext context,
            Phase phase,
            InputPolicies inputPolicies,
            Action<Event> dispatchEvent)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (inputPolicies == null)
                throw new ArgumentNullException(nameof(inputPolicies));
            if (dispatchEvent == null)
                throw new ArgumentNullException(nameof(dispatchEvent));
            if (context.Phase == phase)
                return;

            var currentDefinition = _policy.GetPhase(context.Phase);
            if (!currentDefinition.AllowedTransitions.Contains(phase))
                throw new InvalidOperationException($"Phase transition '{context.Phase}' -> '{phase}' is not allowed.");

            var previous = context.Phase;
            context.Phase = phase;
            if (phase != Phase.Paused)
                _resumePhase = phase;

            inputPolicies.Apply(context);
            dispatchEvent(new Event(Events.PhaseChanged, new PhaseChanged(previous, phase)));
        }

        public void HandleBuiltInCommand(
            SessionContext context,
            Command command,
            Action<Phase> setPhase)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (setPhase == null)
                throw new ArgumentNullException(nameof(setPhase));

            switch (command.Id)
            {
                case Commands.Pause:
                    if (context.Phase == Phase.Paused)
                        return;
                    _resumePhase = context.Phase;
                    setPhase(Phase.Paused);
                    break;
                case Commands.Resume:
                    if (context.Phase != Phase.Paused)
                        return;
                    setPhase(_resumePhase == Phase.Paused ? _policy.ResumeFallbackPhase : _resumePhase);
                    break;
                case Commands.RequestPause:
                    context.WantsPause = true;
                    break;
                case Commands.ClearPauseRequest:
                    context.WantsPause = false;
                    break;
                case Commands.RequestExit:
                    context.WantsExit = true;
                    break;
            }
        }
    }
}

