using System;

namespace TopSpeed.Drive.Session
{
    internal sealed class ExternalEvent
    {
        public ExternalEvent(ExternalEventId id, object? data = null)
        {
            Id = id;
            Data = data;
        }

        public ExternalEventId Id { get; }
        public object? Data { get; }
    }
}

