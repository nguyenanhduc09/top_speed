using System;
using MiniAudioEx.Core.AdvancedAPI;

namespace TS.Audio
{
    public sealed class BusEffect : IDisposable
    {
        private AudioBus? _bus;
        private bool _disposed;

        internal BusEffect(AudioBus bus, MaEffectNode node, AudioEffectProcessCallback process, string? name)
        {
            _bus = bus;
            Node = node;
            Process = process;
            Name = string.IsNullOrWhiteSpace(name) ? "effect" : name!;
        }

        internal MaEffectNode Node { get; }
        internal AudioEffectProcessCallback Process { get; }
        public string Name { get; }
        public bool Enabled { get; set; } = true;
        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _bus?.RemoveEffect(this);
            _bus = null;
            _disposed = true;
        }

        internal void MarkDetached()
        {
            _bus = null;
            _disposed = true;
        }

        internal void DisposeNative()
        {
            Node.Dispose();
        }
    }
}
