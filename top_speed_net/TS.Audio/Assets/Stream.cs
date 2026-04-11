using System;

namespace TS.Audio
{
    public sealed class StreamAsset : SoundAsset
    {
        public override AssetKind Kind => AssetKind.Stream;
        public string Path { get; }

        public StreamAsset(string filePath, string? name = null)
            : base(new FileAsset(filePath, streamFromDisk: true), ownsAsset: true, name: name)
        {
            Path = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }
    }
}
