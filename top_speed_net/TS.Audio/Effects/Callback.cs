using MiniAudioEx.Native;

namespace TS.Audio
{
    public delegate void AudioEffectProcessCallback(NativeArray<float> framesIn, uint frameCountIn, NativeArray<float> framesOut, ref uint frameCountOut, uint channels);
}
