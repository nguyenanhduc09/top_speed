using System;
using System.Collections.Generic;

namespace TopSpeed.Drive.Session
{
    internal sealed class Session
    {
        private readonly Dictionary<SubsystemId, Subsystem> _subsystems;
        private readonly Dictionary<Phase, IReadOnlyList<Subsystem>> _activeSubsystems;
        private readonly Phases _phases;
        private readonly InputPolicies _inputPolicies;
        private readonly Clocks _clocks;
        private readonly Dispatch _events;
        private bool _activeSubsystemsDirty;

        public Session(Policy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            Context = new SessionContext(policy);
            _subsystems = new Dictionary<SubsystemId, Subsystem>();
            _activeSubsystems = new Dictionary<Phase, IReadOnlyList<Subsystem>>();
            _phases = new Phases(policy);
            _inputPolicies = new InputPolicies();
            _clocks = new Clocks();
            _events = new Dispatch();
            _activeSubsystemsDirty = true;
            _phases.Reset(Context, _inputPolicies);

            RegisterCommandHandler(new CommandHandler(new HandlerId("session.core"), int.MinValue, HandleBuiltInCommand));
        }

        public SessionContext Context { get; }

        public void RegisterSubsystem(Subsystem subsystem)
        {
            if (subsystem == null)
                throw new ArgumentNullException(nameof(subsystem));
            if (_subsystems.ContainsKey(subsystem.Id))
                throw new ArgumentException($"Subsystem '{subsystem.Id}' is already registered.", nameof(subsystem));

            _subsystems[subsystem.Id] = subsystem;
            _activeSubsystemsDirty = true;
        }

        public void RegisterEventHandler(EventHandler handler)
        {
            _events.RegisterEventHandler(handler);
        }

        public void RegisterCommandHandler(CommandHandler handler)
        {
            _events.RegisterCommandHandler(handler);
        }

        public void RegisterExternalEventHandler(ExternalEventHandler handler)
        {
            _events.RegisterExternalEventHandler(handler);
        }

        public void Reset()
        {
            Context.Reset();
            _phases.Reset(Context, _inputPolicies);
            _events.Reset();
            _activeSubsystemsDirty = true;
        }

        public void SetPhase(Phase phase)
        {
            _phases.SetPhase(Context, phase, _inputPolicies, sessionEvent => _events.DispatchEvent(Context, sessionEvent));
        }

        public void QueueEvent(Event sessionEvent, float delaySeconds, Clock clock = Clock.Progress)
        {
            _events.QueueEvent(Context, sessionEvent, delaySeconds, clock);
        }

        public void ApplyCommand(Command command)
        {
            _events.ApplyCommand(Context, command);
        }

        public void ApplyExternalEvent(ExternalEvent externalEvent)
        {
            _events.ApplyExternalEvent(Context, externalEvent);
        }

        public void Update(float elapsed)
        {
            _events.DispatchDueEvents(Context);

            var active = GetActiveSubsystems(Context.PhaseDefinition);
            if (active.Count > 0)
            {
                for (var i = 0; i < active.Count; i++)
                    active[i].Update(Context, elapsed);
            }

            _clocks.Advance(Context, elapsed);
        }

        private void HandleBuiltInCommand(SessionContext context, Command command)
        {
            _phases.HandleBuiltInCommand(context, command, SetPhase);
        }

        private IReadOnlyList<Subsystem> GetActiveSubsystems(PhaseDefinition definition)
        {
            if (_activeSubsystemsDirty)
            {
                _activeSubsystems.Clear();
                _activeSubsystemsDirty = false;
            }

            if (_activeSubsystems.TryGetValue(definition.Phase, out var cached))
                return cached;

            if (definition.ActiveSubsystems.Count == 0)
            {
                var empty = Array.Empty<Subsystem>();
                _activeSubsystems[definition.Phase] = empty;
                return empty;
            }

            var active = new List<Subsystem>(definition.ActiveSubsystems.Count);
            foreach (var name in definition.ActiveSubsystems)
            {
                if (_subsystems.TryGetValue(name, out var subsystem))
                    active.Add(subsystem);
            }

            active.Sort((a, b) => a.Order.CompareTo(b.Order));
            _activeSubsystems[definition.Phase] = active;
            return active;
        }
    }
}

