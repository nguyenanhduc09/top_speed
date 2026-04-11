using System;
using MiniAudioEx.Native;

namespace TS.Audio
{
    internal sealed class ProceduralAsset : AudioAsset
    {
        private readonly ProceduralAudioCallback _callback;

        public override int InputChannels { get; }
        public override int InputSampleRate { get; }
        public override float LengthSeconds => 0f;

        public ProceduralAsset(ProceduralAudioCallback callback, uint channels, uint sampleRate)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            InputChannels = channels > 0 ? (int)channels : 1;
            InputSampleRate = sampleRate > 0 ? (int)sampleRate : 44100;
        }

        internal override SourcePlayback CreatePlayback()
        {
            return new ProceduralPlayback(_callback, InputChannels, InputSampleRate);
        }
    }

    internal sealed class ProceduralPlayback : SourcePlayback
    {
        private readonly ProceduralAudioGenerator _generator;
        private readonly int _channels;
        private readonly int _sampleRate;

        public ProceduralPlayback(ProceduralAudioCallback callback, int channels, int sampleRate)
        {
            _generator = new ProceduralAudioGenerator(callback, channels, sampleRate);
            _channels = channels;
            _sampleRate = sampleRate;
        }

        public override bool SupportsSeeking => false;

        public override ma_result Prepare(IntPtr sourceHandle)
        {
            return MiniAudioExNative.ma_ex_audio_source_prepare_from_callback(
                sourceHandle,
                _generator.Proc,
                _generator.UserData,
                (uint)_channels,
                (uint)_sampleRate);
        }

        public override void Dispose()
        {
            _generator.Dispose();
        }
    }
}
