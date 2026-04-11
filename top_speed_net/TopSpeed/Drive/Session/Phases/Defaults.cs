using System;

namespace TopSpeed.Drive.Session
{
    internal static class Defaults
    {
        public static readonly CommandId[] StandardCommands =
        [
            Commands.Pause,
            Commands.Resume,
            Commands.RequestPause,
            Commands.ClearPauseRequest,
            Commands.RequestExit
        ];

        public static readonly SubsystemId[] NoSubsystems = Array.Empty<SubsystemId>();
        public static readonly ExternalEventId[] NoExternalEvents = Array.Empty<ExternalEventId>();
    }
}
