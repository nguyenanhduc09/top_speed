namespace TS.Audio
{
    public sealed class AudioSourceSnapshot
    {
        public string BusName { get; }
        public bool IsPlaying { get; }
        public bool IsSpatialized { get; }
        public bool UsesSteamAudio { get; }
        public int InputChannels { get; }
        public int InputSampleRate { get; }
        public float LengthSeconds { get; }

        public AudioSourceSnapshot(string busName, bool isPlaying, bool isSpatialized, bool usesSteamAudio, int inputChannels, int inputSampleRate, float lengthSeconds)
        {
            BusName = busName;
            IsPlaying = isPlaying;
            IsSpatialized = isSpatialized;
            UsesSteamAudio = usesSteamAudio;
            InputChannels = inputChannels;
            InputSampleRate = inputSampleRate;
            LengthSeconds = lengthSeconds;
        }
    }
}
