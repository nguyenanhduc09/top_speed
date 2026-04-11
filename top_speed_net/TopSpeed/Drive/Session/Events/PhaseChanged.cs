using System;

namespace TopSpeed.Drive.Session
{
    internal sealed class PhaseChanged
    {
        public PhaseChanged(Phase previous, Phase current)
        {
            Previous = previous;
            Current = current;
        }

        public Phase Previous { get; }
        public Phase Current { get; }
    }
}
