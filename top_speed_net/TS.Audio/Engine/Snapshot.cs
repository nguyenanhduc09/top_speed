using System;
using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioEngineSnapshot
    {
        public DateTime CreatedUtc { get; }
        public IReadOnlyList<AudioOutputSnapshot> Outputs { get; }

        public AudioEngineSnapshot(DateTime createdUtc, IReadOnlyList<AudioOutputSnapshot> outputs)
        {
            CreatedUtc = createdUtc;
            Outputs = outputs;
        }
    }
}
