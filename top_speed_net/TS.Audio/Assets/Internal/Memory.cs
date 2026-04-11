using System;
using System.Runtime.InteropServices;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;

namespace TS.Audio
{
    internal sealed class MemoryAsset : AudioAsset
    {
        private readonly GCHandle _dataHandle;

        public ulong DataSize { get; }
        public IntPtr DataPointer => _dataHandle.AddrOfPinnedObject();
        public override int InputChannels { get; }
        public override int InputSampleRate { get; }
        public override float LengthSeconds { get; }

        public MemoryAsset(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Audio data is required.", nameof(data));

            _dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            DataSize = (ulong)data.Length;
            ReadFormat(DataPointer, DataSize, out var channels, out var sampleRate, out var lengthSeconds);
            InputChannels = channels;
            InputSampleRate = sampleRate;
            LengthSeconds = lengthSeconds;
        }

        internal override SourcePlayback CreatePlayback()
        {
            return new MemoryPlayback(DataPointer, DataSize);
        }

        public override void Dispose()
        {
            if (_dataHandle.IsAllocated)
                _dataHandle.Free();
        }

        private static void ReadFormat(IntPtr data, ulong dataSize, out int channels, out int sampleRate, out float lengthSeconds)
        {
            channels = 2;
            sampleRate = 44100;
            lengthSeconds = 0f;

            using var decoder = new MaDecoder();
            if (decoder.IntializeFromMemory(data, dataSize) != ma_result.success)
                return;

            if (decoder.GetDataFormat(out _, out var decodedChannels, out var decodedSampleRate, default, 0) != ma_result.success)
                return;

            if (decodedChannels > 0)
                channels = (int)decodedChannels;
            if (decodedSampleRate > 0)
                sampleRate = (int)decodedSampleRate;

            if (decoder.GetLengthInPCMFrames(out var pcmFrames) == ma_result.success && decodedSampleRate > 0)
                lengthSeconds = (float)(pcmFrames / (double)decodedSampleRate);
        }
    }

    internal sealed class MemoryPlayback : SourcePlayback
    {
        private readonly IntPtr _data;
        private readonly ulong _dataSize;

        public MemoryPlayback(IntPtr data, ulong dataSize)
        {
            _data = data;
            _dataSize = dataSize;
        }

        public override bool SupportsSeeking => true;

        public override ma_result Prepare(IntPtr sourceHandle)
        {
            return MiniAudioExNative.ma_ex_audio_source_prepare_from_memory(sourceHandle, _data, _dataSize);
        }
    }
}
