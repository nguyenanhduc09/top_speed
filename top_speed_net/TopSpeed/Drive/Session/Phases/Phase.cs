namespace TopSpeed.Drive.Session
{
    internal enum Phase
    {
        Initializing = 0,
        Countdown = 1,
        Running = 2,
        Paused = 3,
        Finishing = 4,
        Finished = 5,
        Aborted = 6
    }
}
