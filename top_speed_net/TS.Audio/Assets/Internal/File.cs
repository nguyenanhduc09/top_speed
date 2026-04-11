using System;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;

namespace TS.Audio
{
    internal sealed class FileAsset : AudioAsset
    {
        public string Path { get; }
        public bool StreamFromDisk { get; }
        public override int InputChannels { get; }
        public override int InputSampleRate { get; }
        public override float LengthSeconds { get; }

        public FileAsset(string path, bool streamFromDisk)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path is required.", nameof(path));

            Path = path;
            StreamFromDisk = streamFromDisk;
            ReadFormat(path, out var channels, out var sampleRate, out var lengthSeconds);
            InputChannels = channels;
            InputSampleRate = sampleRate;
            LengthSeconds = lengthSeconds;
        }

        internal override SourcePlayback CreatePlayback()
        {
            return new FilePlayback(Path, StreamFromDisk);
        }

        private static void ReadFormat(string path, out int channels, out int sampleRate, out float lengthSeconds)
        {
            channels = 2;
            sampleRate = 44100;
            lengthSeconds = 0f;

            using var decoder = new MaDecoder();
            if (decoder.InitializeFromFile(path) != ma_result.success)
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

    internal sealed class FilePlayback : SourcePlayback
    {
        private readonly string _path;
        private readonly bool _streamFromDisk;

        public FilePlayback(string path, bool streamFromDisk)
        {
            _path = path;
            _streamFromDisk = streamFromDisk;
        }

        public override bool SupportsSeeking => true;

        public override ma_result Prepare(IntPtr sourceHandle)
        {
            return MiniAudioExNative.ma_ex_audio_source_prepare_from_file(sourceHandle, _path, _streamFromDisk ? 1u : 0u);
        }
    }
}
