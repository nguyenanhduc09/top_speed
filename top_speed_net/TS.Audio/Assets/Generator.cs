using System;

namespace TS.Audio
{
    public sealed class GeneratorAsset : SoundAsset
    {
        public override AssetKind Kind => AssetKind.Procedural;
        public uint Channels { get; }
        public uint SampleRate { get; }

        public GeneratorAsset(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, string? name = null)
            : base(new ProceduralAsset(callback ?? throw new ArgumentNullException(nameof(callback)), channels, sampleRate), ownsAsset: true, name: name)
        {
            Channels = channels;
            SampleRate = sampleRate;
        }
    }
}
