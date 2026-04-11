namespace TS.Audio
{
    public sealed class BufferAsset : SoundAsset
    {
        public override AssetKind Kind => AssetKind.Buffer;

        public BufferAsset(byte[] data, string? name = null)
            : base(new MemoryAsset(data), ownsAsset: true, name: name)
        {
        }
    }
}
