using System;
using MiniAudioEx.Native;

namespace TS.Audio
{
    internal abstract class SourcePlayback : IDisposable
    {
        public abstract bool SupportsSeeking { get; }
        public abstract ma_result Prepare(IntPtr sourceHandle);

        public virtual void Dispose()
        {
        }
    }
}
