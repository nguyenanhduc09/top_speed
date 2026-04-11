using System;

namespace TopSpeed.Drive.Session
{
    internal sealed class EventHandler
    {
        public EventHandler(HandlerId id, int order, Action<SessionContext, Event> handle)
        {
            Id = id;
            Order = order;
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public HandlerId Id { get; }
        public int Order { get; }
        public Action<SessionContext, Event> Handle { get; }
    }
}

