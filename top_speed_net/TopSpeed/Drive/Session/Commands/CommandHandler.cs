using System;

namespace TopSpeed.Drive.Session
{
    internal sealed class CommandHandler
    {
        public CommandHandler(HandlerId id, int order, Action<SessionContext, Command> handle)
        {
            Id = id;
            Order = order;
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public HandlerId Id { get; }
        public int Order { get; }
        public Action<SessionContext, Command> Handle { get; }
    }
}

