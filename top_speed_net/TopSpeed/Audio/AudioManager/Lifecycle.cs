namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager
    {
        public void Dispose()
        {
            StopUpdateThread();
            ClearCachedPaths();
            _engine.Dispose();
        }
    }
}

