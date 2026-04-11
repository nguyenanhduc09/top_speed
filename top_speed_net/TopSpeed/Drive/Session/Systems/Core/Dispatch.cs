using System;
using System.Collections.Generic;
using System.Linq;

namespace TopSpeed.Drive.Session
{
    internal sealed class Dispatch
    {
        private sealed class ScheduledEvent
        {
            public ScheduledEvent(Event sessionEvent, Clock clock, float dueSeconds, long sequence)
            {
                SessionEvent = sessionEvent;
                Clock = clock;
                DueSeconds = dueSeconds;
                Sequence = sequence;
            }

            public Event SessionEvent { get; }
            public Clock Clock { get; }
            public float DueSeconds { get; }
            public long Sequence { get; }
        }

        private readonly List<EventHandler> _eventHandlers;
        private readonly List<CommandHandler> _commandHandlers;
        private readonly List<ExternalEventHandler> _externalEventHandlers;
        private readonly List<ScheduledEvent> _scheduledEvents;
        private readonly List<ScheduledEvent> _dueEvents;
        private long _nextSequence;

        public Dispatch()
        {
            _eventHandlers = new List<EventHandler>();
            _commandHandlers = new List<CommandHandler>();
            _externalEventHandlers = new List<ExternalEventHandler>();
            _scheduledEvents = new List<ScheduledEvent>();
            _dueEvents = new List<ScheduledEvent>();
        }

        public void RegisterEventHandler(EventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _eventHandlers.Add(handler);
            _eventHandlers.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void RegisterCommandHandler(CommandHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _commandHandlers.Add(handler);
            _commandHandlers.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void RegisterExternalEventHandler(ExternalEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _externalEventHandlers.Add(handler);
            _externalEventHandlers.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void Reset()
        {
            _nextSequence = 0;
            _scheduledEvents.Clear();
            _dueEvents.Clear();
        }

        public void QueueEvent(SessionContext context, Event sessionEvent, float delaySeconds, Clock clock = Clock.Progress)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (sessionEvent == null)
                throw new ArgumentNullException(nameof(sessionEvent));

            var baseSeconds = clock == Clock.Progress ? context.ProgressSeconds : context.RuntimeSeconds;
            _scheduledEvents.Add(new ScheduledEvent(sessionEvent, clock, baseSeconds + Math.Max(0f, delaySeconds), _nextSequence++));
        }

        public void ApplyCommand(SessionContext context, Command command)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (!context.PhaseDefinition.AllowedCommands.Contains(command.Id))
                return;

            for (var i = 0; i < _commandHandlers.Count; i++)
                _commandHandlers[i].Handle(context, command);
        }

        public void ApplyExternalEvent(SessionContext context, ExternalEvent externalEvent)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (externalEvent == null)
                throw new ArgumentNullException(nameof(externalEvent));
            if (!context.PhaseDefinition.AllowedExternalEvents.Contains(externalEvent.Id))
                return;

            for (var i = 0; i < _externalEventHandlers.Count; i++)
                _externalEventHandlers[i].Handle(context, externalEvent);
        }

        public void DispatchDueEvents(SessionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            CollectDueEvents(context);
            for (var i = 0; i < _dueEvents.Count; i++)
                DispatchEvent(context, _dueEvents[i].SessionEvent);
        }

        public void DispatchEvent(SessionContext context, Event sessionEvent)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (sessionEvent == null)
                throw new ArgumentNullException(nameof(sessionEvent));

            for (var i = 0; i < _eventHandlers.Count; i++)
                _eventHandlers[i].Handle(context, sessionEvent);
        }

        private void CollectDueEvents(SessionContext context)
        {
            _dueEvents.Clear();
            for (var i = _scheduledEvents.Count - 1; i >= 0; i--)
            {
                var scheduled = _scheduledEvents[i];
                var currentSeconds = scheduled.Clock == Clock.Progress ? context.ProgressSeconds : context.RuntimeSeconds;
                if (scheduled.DueSeconds <= currentSeconds)
                {
                    _scheduledEvents.RemoveAt(i);
                    _dueEvents.Add(scheduled);
                }
            }

            if (_dueEvents.Count > 1)
                _dueEvents.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
        }
    }
}

