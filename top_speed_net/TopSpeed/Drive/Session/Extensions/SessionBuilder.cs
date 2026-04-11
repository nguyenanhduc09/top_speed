using System;

namespace TopSpeed.Drive.Session
{
    internal sealed class SessionBuilder
    {
        private readonly Session _session;

        public SessionBuilder(Policy policy)
        {
            _session = new Session(policy);
        }

        public SessionBuilder UseExtension(IExtension extension)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            extension.Configure(this);
            return this;
        }

        public SessionBuilder AddSubsystem(Subsystem subsystem)
        {
            _session.RegisterSubsystem(subsystem ?? throw new ArgumentNullException(nameof(subsystem)));
            return this;
        }

        public SessionBuilder AddSubsystems(params Subsystem[] subsystems)
        {
            if (subsystems == null)
                throw new ArgumentNullException(nameof(subsystems));

            for (var i = 0; i < subsystems.Length; i++)
                AddSubsystem(subsystems[i]);

            return this;
        }

        public SessionBuilder AddEventHandler(HandlerId id, int order, Action<SessionContext, Event> handle)
        {
            _session.RegisterEventHandler(new EventHandler(id, order, handle));
            return this;
        }

        public SessionBuilder AddCommandHandler(HandlerId id, int order, Action<SessionContext, Command> handle)
        {
            _session.RegisterCommandHandler(new CommandHandler(id, order, handle));
            return this;
        }

        public SessionBuilder AddExternalEventHandler(HandlerId id, int order, Action<SessionContext, ExternalEvent> handle)
        {
            _session.RegisterExternalEventHandler(new ExternalEventHandler(id, order, handle));
            return this;
        }

        public SessionBuilder AddState<T>(T value) where T : class
        {
            _session.Context.SetState(value);
            return this;
        }

        public Session Build()
        {
            return _session;
        }
    }
}

