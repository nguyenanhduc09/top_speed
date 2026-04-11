using System;

namespace TopSpeed.Drive.Session
{
    internal sealed class ExternalEventHandler
    {
        public ExternalEventHandler(HandlerId id, int order, Action<SessionContext, ExternalEvent> handle)
        {
            Id = id;
            Order = order;
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public HandlerId Id { get; }
        public int Order { get; }
        public Action<SessionContext, ExternalEvent> Handle { get; }
    }
}

