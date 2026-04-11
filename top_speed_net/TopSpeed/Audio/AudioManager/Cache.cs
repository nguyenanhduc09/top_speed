namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager
    {
        public bool TryResolvePath(string path, out string fullPath)
        {
            return _engine.TryResolveFile(path, out fullPath!);
        }

        private void ClearCachedPaths()
        {
        }
    }
}

