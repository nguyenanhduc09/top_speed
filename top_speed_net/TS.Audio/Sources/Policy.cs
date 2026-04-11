using System.Numerics;

namespace TS.Audio
{
    public class PlaybackPolicy
    {
        public bool? Spatialize { get; set; }
        public bool? UseHrtf { get; set; }
        public bool? Loop { get; set; }
        public float? FadeInSeconds { get; set; }
        public float? Volume { get; set; }
        public float? Pitch { get; set; }
        public float? Pan { get; set; }
        public bool? StereoWidening { get; set; }
        public Vector3? Position { get; set; }
        public Vector3? Velocity { get; set; }
        public float? CurveDistanceScaler { get; set; }
        public float? DopplerFactor { get; set; }
        public RoomAcoustics? RoomAcoustics { get; set; }
        public DistanceModel? DistanceModel { get; set; }
        public float? RefDistance { get; set; }
        public float? MaxDistance { get; set; }
        public float? RollOff { get; set; }

        public PlaybackPolicy Clone()
        {
            return new PlaybackPolicy
            {
                Spatialize = Spatialize,
                UseHrtf = UseHrtf,
                Loop = Loop,
                FadeInSeconds = FadeInSeconds,
                Volume = Volume,
                Pitch = Pitch,
                Pan = Pan,
                StereoWidening = StereoWidening,
                Position = Position,
                Velocity = Velocity,
                CurveDistanceScaler = CurveDistanceScaler,
                DopplerFactor = DopplerFactor,
                RoomAcoustics = RoomAcoustics,
                DistanceModel = DistanceModel,
                RefDistance = RefDistance,
                MaxDistance = MaxDistance,
                RollOff = RollOff
            };
        }
    }
}
