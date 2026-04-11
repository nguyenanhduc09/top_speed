using System;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class Exit : Subsystem
    {
        private readonly Func<bool> _shouldRequestExit;
        private readonly Action _requestExit;

        public Exit(
            string name,
            int order,
            Func<bool> shouldRequestExit,
            Action requestExit)
            : base(name, order)
        {
            _shouldRequestExit = shouldRequestExit ?? throw new ArgumentNullException(nameof(shouldRequestExit));
            _requestExit = requestExit ?? throw new ArgumentNullException(nameof(requestExit));
        }

        public override void Update(SessionContext context, float elapsed)
        {
            if (_shouldRequestExit())
                _requestExit();
        }
    }
}
