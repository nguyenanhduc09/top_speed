namespace TS.Audio
{
    public sealed class Clip : SoundAsset
    {
        public override AssetKind Kind => AssetKind.Clip;

        public Clip(string filePath, bool streamFromDisk = true)
            : this(new FileAsset(filePath, streamFromDisk), ownsAsset: true, name: null)
        {
        }

        public Clip(byte[] waveData, string? name = null)
            : this(new MemoryAsset(waveData), ownsAsset: true, name: name)
        {
        }

        internal Clip(AudioAsset asset, bool ownsAsset, string? name)
            : base(asset, ownsAsset, name)
        {
        }
    }
}
