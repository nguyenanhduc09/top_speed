using System;

namespace TS.Audio
{
    public abstract class SoundAsset : IDisposable
    {
        private readonly bool _ownsAsset;
        private bool _disposed;

        internal AudioAsset Asset { get; }
        public string? Name { get; }
        public abstract AssetKind Kind { get; }
        public int InputChannels => Asset.InputChannels;
        public int InputSampleRate => Asset.InputSampleRate;
        public float LengthSeconds => Asset.LengthSeconds;

        internal SoundAsset(AudioAsset asset, bool ownsAsset, string? name)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _ownsAsset = ownsAsset;
            Name = name;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_ownsAsset)
                Asset.Dispose();
        }
    }
}
