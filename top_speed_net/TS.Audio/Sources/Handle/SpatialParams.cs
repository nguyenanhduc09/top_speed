namespace TS.Audio
{
    internal sealed class AudioSourceSpatialParams
    {
        public float PosX;
        public float PosY;
        public float PosZ;
        public float VelX;
        public float VelY;
        public float VelZ;
        public float RefDistance = 1.0f;
        public float MaxDistance = 10000.0f;
        public float RollOff = 1.0f;
        public float Occlusion;
        public float AirAbsLow = 1.0f;
        public float AirAbsMid = 1.0f;
        public float AirAbsHigh = 1.0f;
        public float TransLow;
        public float TransMid;
        public float TransHigh;
        public int StereoWidening;
        public int SimulationFlags;
        public float ReverbTimeLow;
        public float ReverbTimeMid;
        public float ReverbTimeHigh;
        public float ReverbEqLow = 1.0f;
        public float ReverbEqMid = 1.0f;
        public float ReverbEqHigh = 1.0f;
        public int ReverbDelay;
        public int RoomFlags;
        public float RoomReverbTimeSeconds;
        public float RoomReverbGain;
        public float RoomHfDecayRatio = 1f;
        public float RoomLateReverbGain;
        public float RoomDiffusion;
        public float RoomAirAbsorptionScale = 1f;
        public float RoomOcclusionScale = 1f;
        public float RoomTransmissionScale = 1f;
        public float RoomOcclusionOverride = float.NaN;
        public float RoomTransmissionOverrideLow = float.NaN;
        public float RoomTransmissionOverrideMid = float.NaN;
        public float RoomTransmissionOverrideHigh = float.NaN;
        public float RoomAirAbsorptionOverrideLow = float.NaN;
        public float RoomAirAbsorptionOverrideMid = float.NaN;
        public float RoomAirAbsorptionOverrideHigh = float.NaN;
        public DistanceModel DistanceModel = DistanceModel.Inverse;

        public const int SimOcclusion = 1 << 0;
        public const int SimTransmission = 1 << 1;
        public const int SimAirAbsorption = 1 << 2;
        public const int SimReflections = 1 << 3;
        public const int RoomHasProfile = 1 << 8;
    }
}
