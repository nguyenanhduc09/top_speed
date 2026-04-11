using System;

namespace TopSpeed.Drive.Session
{
    internal abstract class Subsystem
    {
        protected Subsystem(string name, int order)
        {
            Id = new SubsystemId(name);
            Order = order;
        }

        public SubsystemId Id { get; }
        public int Order { get; }

        public abstract void Update(SessionContext context, float elapsed);
    }
}

