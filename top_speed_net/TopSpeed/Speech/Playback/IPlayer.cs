namespace TopSpeed.Speech.Playback
{
    internal interface IPlayer
    {
        bool IsSpeaking { get; }
        void Write(float[] samples, int channels, int sampleRate, bool interrupt);
        void Stop();
    }
}
