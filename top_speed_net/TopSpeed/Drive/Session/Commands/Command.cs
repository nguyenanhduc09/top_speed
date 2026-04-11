using System;

namespace TopSpeed.Drive.Session
{
    internal enum CommandId
    {
        Pause,
        Resume,
        RequestPause,
        ClearPauseRequest,
        RequestExit
    }

    internal sealed class Command
    {
        public Command(CommandId id, object? data = null)
        {
            Id = id;
            Data = data;
        }

        public CommandId Id { get; }
        public object? Data { get; }
    }
}

