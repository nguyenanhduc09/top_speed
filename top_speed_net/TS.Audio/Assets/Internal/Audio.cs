using System;

namespace TS.Audio
{
    internal abstract class AudioAsset : IDisposable
    {
        public abstract int InputChannels { get; }
        public abstract int InputSampleRate { get; }
        public abstract float LengthSeconds { get; }
        internal abstract SourcePlayback CreatePlayback();

        public virtual void Dispose()
        {
        }
    }
}
