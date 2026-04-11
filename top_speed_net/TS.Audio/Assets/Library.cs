using System;
using System.Collections.Generic;
using System.IO;

namespace TS.Audio
{
    public sealed class AssetLibrary : IDisposable
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, SoundAsset> _cache = new Dictionary<string, SoundAsset>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _pathCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public IReadOnlyCollection<string> Keys
        {
            get
            {
                lock (_sync)
                    return new List<string>(_cache.Keys).AsReadOnly();
            }
        }

        public bool TryGet(string path, AssetKind kind, out SoundAsset asset)
        {
            lock (_sync)
            {
                return _cache.TryGetValue(NormalizeKey(path, kind, streamFromDisk: kind != AssetKind.Clip), out asset!);
            }
        }

        public bool TryResolveFile(string path, out string fullPath)
        {
            fullPath = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            ThrowIfDisposed();
            fullPath = Path.GetFullPath(path);
            lock (_sync)
            {
                if (_pathCache.TryGetValue(fullPath, out var exists))
                    return exists;

                exists = File.Exists(fullPath);
                _pathCache[fullPath] = exists;
                return exists;
            }
        }

        public Clip LoadClip(string path, bool streamFromDisk = true, bool cache = true)
        {
            return (Clip)Load(path, AssetKind.Clip, cache, () => new Clip(path, streamFromDisk), streamFromDisk);
        }

        public StreamAsset LoadStream(string path, bool cache = true)
        {
            return (StreamAsset)Load(path, AssetKind.Stream, cache, () => new StreamAsset(path), streamFromDisk: true);
        }

        public BufferAsset CreateBuffer(byte[] data, string? name = null)
        {
            ThrowIfDisposed();
            return new BufferAsset(data, name);
        }

        public GeneratorAsset CreateProcedural(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, string? name = null)
        {
            ThrowIfDisposed();
            return new GeneratorAsset(callback, channels, sampleRate, name);
        }

        public bool Unload(string path, AssetKind kind, bool streamFromDisk = true)
        {
            lock (_sync)
            {
                var key = NormalizeKey(path, kind, streamFromDisk);
                if (!_cache.TryGetValue(key, out var asset))
                    return false;

                _cache.Remove(key);
                asset.Dispose();
                return true;
            }
        }

        private SoundAsset Load(string path, AssetKind kind, bool cache, Func<SoundAsset> factory, bool streamFromDisk)
        {
            ThrowIfDisposed();
            var key = NormalizeKey(path, kind, streamFromDisk);

            lock (_sync)
            {
                if (_cache.TryGetValue(key, out var existing))
                    return existing;
            }

            var asset = factory();
            if (!cache)
                return asset;

            lock (_sync)
            {
                if (_cache.TryGetValue(key, out var existing))
                {
                    asset.Dispose();
                    return existing;
                }

                _cache.Add(key, asset);
                return asset;
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                foreach (var asset in _cache.Values)
                    asset.Dispose();
                _cache.Clear();
                _pathCache.Clear();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Clear();
        }

        private static string NormalizeKey(string path, AssetKind kind, bool streamFromDisk)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Asset path is required.", nameof(path));

            var mode = kind == AssetKind.Clip ? (streamFromDisk ? "stream" : "decode") : kind.ToString();
            return Path.GetFullPath(path) + "|" + mode;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AssetLibrary));
        }
    }
}
