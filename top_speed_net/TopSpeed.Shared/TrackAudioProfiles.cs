using System;
using System.Collections.Generic;
using System.Numerics;

namespace TopSpeed.Data
{
    public enum TrackSoundSourceType
    {
        Ambient = 0,
        Static = 1,
        Moving = 2,
        Random = 3
    }

    public enum TrackSoundRandomMode
    {
        OnStart = 0,
        PerArea = 1
    }

    public sealed class TrackSoundSourceDefinition
    {
        private static readonly IReadOnlyList<string> EmptyList = Array.Empty<string>();

        public TrackSoundSourceDefinition(
            string id,
            TrackSoundSourceType type,
            string? path,
            IReadOnlyList<string>? variantPaths,
            IReadOnlyList<string>? variantSourceIds,
            TrackSoundRandomMode randomMode,
            bool loop,
            float volume,
            bool spatial,
            bool allowHrtf,
            float fadeInSeconds,
            float fadeOutSeconds,
            float? crossfadeSeconds,
            float pitch,
            float pan,
            float? minDistance,
            float? maxDistance,
            float? rolloff,
            bool global,
            string? startAreaId,
            string? endAreaId,
            Vector3? startPosition,
            float? startRadiusMeters,
            Vector3? endPosition,
            float? endRadiusMeters,
            Vector3? position,
            float? speedMetersPerSecond)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Sound source id is required.", nameof(id));

            Id = id.Trim();
            Type = type;
            Path = string.IsNullOrWhiteSpace(path) ? null : path?.Trim();
            VariantPaths = variantPaths ?? EmptyList;
            VariantSourceIds = variantSourceIds ?? EmptyList;
            RandomMode = randomMode;
            Loop = loop;
            Volume = Clamp01(volume);
            Spatial = spatial;
            AllowHrtf = allowHrtf;
            FadeInSeconds = Math.Max(0f, fadeInSeconds);
            FadeOutSeconds = Math.Max(0f, fadeOutSeconds);
            CrossfadeSeconds = crossfadeSeconds.HasValue ? Math.Max(0f, crossfadeSeconds.Value) : (float?)null;
            Pitch = pitch <= 0f ? 1.0f : pitch;
            Pan = ClampPan(pan);
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            Rolloff = rolloff;
            Global = global;
            StartAreaId = string.IsNullOrWhiteSpace(startAreaId) ? null : startAreaId?.Trim();
            EndAreaId = string.IsNullOrWhiteSpace(endAreaId) ? null : endAreaId?.Trim();
            StartPosition = startPosition;
            StartRadiusMeters = startRadiusMeters;
            EndPosition = endPosition;
            EndRadiusMeters = endRadiusMeters;
            Position = position;
            SpeedMetersPerSecond = speedMetersPerSecond;
        }

        public string Id { get; }
        public TrackSoundSourceType Type { get; }
        public string? Path { get; }
        public IReadOnlyList<string> VariantPaths { get; }
        public IReadOnlyList<string> VariantSourceIds { get; }
        public TrackSoundRandomMode RandomMode { get; }
        public bool Loop { get; }
        public float Volume { get; }
        public bool Spatial { get; }
        public bool AllowHrtf { get; }
        public float FadeInSeconds { get; }
        public float FadeOutSeconds { get; }
        public float? CrossfadeSeconds { get; }
        public float Pitch { get; }
        public float Pan { get; }
        public float? MinDistance { get; }
        public float? MaxDistance { get; }
        public float? Rolloff { get; }
        public bool Global { get; }
        public string? StartAreaId { get; }
        public string? EndAreaId { get; }
        public Vector3? StartPosition { get; }
        public float? StartRadiusMeters { get; }
        public Vector3? EndPosition { get; }
        public float? EndRadiusMeters { get; }
        public Vector3? Position { get; }
        public float? SpeedMetersPerSecond { get; }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static float ClampPan(float value)
        {
            if (value < -1f)
                return -1f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }

    public sealed class TrackRoomDefinition
    {
        public TrackRoomDefinition(
            string id,
            string? name,
            float reverbTimeSeconds,
            float reverbGain,
            float hfDecayRatio,
            float lateReverbGain,
            float diffusion,
            float airAbsorption,
            float occlusionScale,
            float transmissionScale,
            float? occlusionOverride = null,
            float? transmissionOverrideLow = null,
            float? transmissionOverrideMid = null,
            float? transmissionOverrideHigh = null,
            float? airAbsorptionOverrideLow = null,
            float? airAbsorptionOverrideMid = null,
            float? airAbsorptionOverrideHigh = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Room id is required.", nameof(id));

            Id = id.Trim();
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            ReverbTimeSeconds = Math.Max(0f, reverbTimeSeconds);
            ReverbGain = Clamp01(reverbGain);
            HfDecayRatio = Clamp01(hfDecayRatio);
            LateReverbGain = Clamp01(lateReverbGain);
            Diffusion = Clamp01(diffusion);
            AirAbsorption = Clamp01(airAbsorption);
            OcclusionScale = Clamp01(occlusionScale);
            TransmissionScale = Clamp01(transmissionScale);
            OcclusionOverride = ClampOptional01(occlusionOverride);
            TransmissionOverrideLow = ClampOptional01(transmissionOverrideLow);
            TransmissionOverrideMid = ClampOptional01(transmissionOverrideMid);
            TransmissionOverrideHigh = ClampOptional01(transmissionOverrideHigh);
            AirAbsorptionOverrideLow = ClampOptional01(airAbsorptionOverrideLow);
            AirAbsorptionOverrideMid = ClampOptional01(airAbsorptionOverrideMid);
            AirAbsorptionOverrideHigh = ClampOptional01(airAbsorptionOverrideHigh);
        }

        public string Id { get; }
        public string? Name { get; }
        public float ReverbTimeSeconds { get; }
        public float ReverbGain { get; }
        public float HfDecayRatio { get; }
        public float LateReverbGain { get; }
        public float Diffusion { get; }
        public float AirAbsorption { get; }
        public float OcclusionScale { get; }
        public float TransmissionScale { get; }
        public float? OcclusionOverride { get; }
        public float? TransmissionOverrideLow { get; }
        public float? TransmissionOverrideMid { get; }
        public float? TransmissionOverrideHigh { get; }
        public float? AirAbsorptionOverrideLow { get; }
        public float? AirAbsorptionOverrideMid { get; }
        public float? AirAbsorptionOverrideHigh { get; }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static float? ClampOptional01(float? value)
        {
            if (!value.HasValue)
                return null;
            return Clamp01(value.Value);
        }
    }

    public sealed class TrackRoomOverrides
    {
        public float? ReverbTimeSeconds { get; set; }
        public float? ReverbGain { get; set; }
        public float? HfDecayRatio { get; set; }
        public float? LateReverbGain { get; set; }
        public float? Diffusion { get; set; }
        public float? AirAbsorption { get; set; }
        public float? OcclusionScale { get; set; }
        public float? TransmissionScale { get; set; }
        public float? OcclusionOverride { get; set; }
        public float? TransmissionOverrideLow { get; set; }
        public float? TransmissionOverrideMid { get; set; }
        public float? TransmissionOverrideHigh { get; set; }
        public float? AirAbsorptionOverrideLow { get; set; }
        public float? AirAbsorptionOverrideMid { get; set; }
        public float? AirAbsorptionOverrideHigh { get; set; }

        public bool HasAny =>
            ReverbTimeSeconds.HasValue ||
            ReverbGain.HasValue ||
            HfDecayRatio.HasValue ||
            LateReverbGain.HasValue ||
            Diffusion.HasValue ||
            AirAbsorption.HasValue ||
            OcclusionScale.HasValue ||
            TransmissionScale.HasValue ||
            OcclusionOverride.HasValue ||
            TransmissionOverrideLow.HasValue ||
            TransmissionOverrideMid.HasValue ||
            TransmissionOverrideHigh.HasValue ||
            AirAbsorptionOverrideLow.HasValue ||
            AirAbsorptionOverrideMid.HasValue ||
            AirAbsorptionOverrideHigh.HasValue;
    }

    public static class TrackRoomLibrary
    {
        private struct RoomValues
        {
            public float ReverbTimeSeconds;
            public float ReverbGain;
            public float HfDecayRatio;
            public float LateReverbGain;
            public float Diffusion;
            public float AirAbsorption;
            public float OcclusionScale;
            public float TransmissionScale;
        }

        private static readonly Dictionary<string, RoomValues> Presets =
            new Dictionary<string, RoomValues>(StringComparer.OrdinalIgnoreCase)
            {
                ["outdoor_open"] = R(0.35f, 0.08f, 0.85f, 0.08f, 0.20f, 0.65f, 0.35f, 0.75f),
                ["outdoor_field"] = R(0.45f, 0.10f, 0.82f, 0.10f, 0.25f, 0.62f, 0.38f, 0.72f),
                ["outdoor_urban"] = R(0.90f, 0.22f, 0.70f, 0.24f, 0.45f, 0.48f, 0.55f, 0.52f),
                ["outdoor_suburban"] = R(0.75f, 0.18f, 0.74f, 0.20f, 0.38f, 0.50f, 0.48f, 0.58f),
                ["outdoor_forest"] = R(0.70f, 0.16f, 0.52f, 0.18f, 0.32f, 0.82f, 0.60f, 0.60f),
                ["outdoor_mountains"] = R(1.80f, 0.34f, 0.52f, 0.36f, 0.42f, 0.42f, 0.55f, 0.52f),
                ["outdoor_desert"] = R(0.55f, 0.12f, 0.76f, 0.12f, 0.26f, 0.58f, 0.42f, 0.66f),
                ["outdoor_snowfield"] = R(0.90f, 0.20f, 0.62f, 0.24f, 0.34f, 0.70f, 0.45f, 0.62f),
                ["outdoor_coast"] = R(0.85f, 0.20f, 0.68f, 0.22f, 0.36f, 0.52f, 0.46f, 0.60f),
                ["outdoor_valley"] = R(1.60f, 0.30f, 0.56f, 0.34f, 0.46f, 0.46f, 0.58f, 0.52f),

                ["tunnel_short"] = R(1.10f, 0.48f, 0.62f, 0.52f, 0.72f, 0.22f, 0.80f, 0.32f),
                ["tunnel_medium"] = R(1.80f, 0.60f, 0.56f, 0.62f, 0.78f, 0.20f, 0.86f, 0.26f),
                ["tunnel_long"] = R(2.70f, 0.72f, 0.50f, 0.76f, 0.82f, 0.18f, 0.90f, 0.22f),
                ["tunnel_concrete"] = R(2.10f, 0.66f, 0.54f, 0.70f, 0.80f, 0.20f, 0.88f, 0.24f),
                ["tunnel_brick"] = R(1.70f, 0.58f, 0.58f, 0.62f, 0.76f, 0.22f, 0.84f, 0.30f),
                ["tunnel_metal"] = R(2.00f, 0.70f, 0.46f, 0.74f, 0.84f, 0.16f, 0.88f, 0.22f),
                ["tunnel_stone"] = R(2.40f, 0.68f, 0.50f, 0.72f, 0.80f, 0.18f, 0.90f, 0.24f),

                ["underpass_small"] = R(0.95f, 0.38f, 0.62f, 0.42f, 0.62f, 0.26f, 0.78f, 0.34f),
                ["underpass_large"] = R(1.35f, 0.46f, 0.56f, 0.52f, 0.68f, 0.24f, 0.82f, 0.30f),
                ["overhang"] = R(0.75f, 0.30f, 0.66f, 0.34f, 0.56f, 0.28f, 0.72f, 0.38f),
                ["bridge_truss"] = R(0.65f, 0.24f, 0.64f, 0.26f, 0.44f, 0.34f, 0.60f, 0.46f),

                ["garage_small"] = R(0.95f, 0.40f, 0.64f, 0.44f, 0.62f, 0.30f, 0.72f, 0.34f),
                ["garage_medium"] = R(1.30f, 0.48f, 0.60f, 0.52f, 0.68f, 0.28f, 0.74f, 0.30f),
                ["garage_large"] = R(1.80f, 0.56f, 0.58f, 0.60f, 0.72f, 0.26f, 0.76f, 0.28f),
                ["parking_open"] = R(0.70f, 0.20f, 0.70f, 0.22f, 0.38f, 0.40f, 0.48f, 0.58f),
                ["parking_covered"] = R(1.20f, 0.44f, 0.60f, 0.48f, 0.64f, 0.28f, 0.72f, 0.34f),
                ["parking_underground"] = R(1.90f, 0.62f, 0.54f, 0.66f, 0.76f, 0.22f, 0.84f, 0.24f),

                ["warehouse_small"] = R(1.10f, 0.38f, 0.62f, 0.42f, 0.66f, 0.30f, 0.70f, 0.34f),
                ["warehouse_medium"] = R(1.70f, 0.50f, 0.56f, 0.56f, 0.74f, 0.28f, 0.74f, 0.30f),
                ["warehouse_large"] = R(2.40f, 0.62f, 0.50f, 0.68f, 0.80f, 0.24f, 0.78f, 0.26f),
                ["factory_hall"] = R(2.20f, 0.60f, 0.48f, 0.66f, 0.78f, 0.24f, 0.80f, 0.26f),
                ["machine_shop"] = R(1.30f, 0.44f, 0.54f, 0.50f, 0.70f, 0.26f, 0.76f, 0.30f),

                ["hangar_small"] = R(2.00f, 0.56f, 0.54f, 0.60f, 0.76f, 0.24f, 0.72f, 0.30f),
                ["hangar_large"] = R(3.10f, 0.68f, 0.48f, 0.74f, 0.82f, 0.22f, 0.76f, 0.26f),
                ["airport_terminal"] = R(1.80f, 0.52f, 0.58f, 0.58f, 0.74f, 0.28f, 0.66f, 0.36f),
                ["subway_station"] = R(2.30f, 0.64f, 0.50f, 0.70f, 0.80f, 0.22f, 0.84f, 0.24f),
                ["rail_station"] = R(1.90f, 0.54f, 0.56f, 0.60f, 0.74f, 0.26f, 0.72f, 0.32f),

                ["corridor_short"] = R(0.85f, 0.36f, 0.62f, 0.40f, 0.58f, 0.30f, 0.72f, 0.34f),
                ["corridor_long"] = R(1.40f, 0.50f, 0.56f, 0.56f, 0.68f, 0.26f, 0.80f, 0.28f),
                ["stairwell_concrete"] = R(1.60f, 0.54f, 0.54f, 0.58f, 0.70f, 0.26f, 0.78f, 0.28f),
                ["basement_low"] = R(1.10f, 0.44f, 0.58f, 0.48f, 0.66f, 0.28f, 0.76f, 0.30f),
                ["basement_large"] = R(1.90f, 0.58f, 0.52f, 0.64f, 0.76f, 0.24f, 0.82f, 0.26f),
                ["bunker"] = R(2.10f, 0.64f, 0.46f, 0.70f, 0.80f, 0.20f, 0.90f, 0.20f),
                ["vault"] = R(2.60f, 0.72f, 0.42f, 0.78f, 0.84f, 0.18f, 0.92f, 0.18f),

                ["hall_small"] = R(1.10f, 0.40f, 0.64f, 0.44f, 0.70f, 0.30f, 0.68f, 0.34f),
                ["hall_medium"] = R(1.70f, 0.52f, 0.58f, 0.56f, 0.78f, 0.28f, 0.72f, 0.30f),
                ["hall_large"] = R(2.70f, 0.62f, 0.50f, 0.66f, 0.82f, 0.24f, 0.78f, 0.26f),
                ["arena_indoor"] = R(3.00f, 0.66f, 0.48f, 0.72f, 0.84f, 0.24f, 0.70f, 0.32f),
                ["stadium_open"] = R(1.50f, 0.45f, 0.60f, 0.50f, 0.70f, 0.40f, 0.40f, 0.60f),
                ["stadium_closed"] = R(2.80f, 0.64f, 0.50f, 0.70f, 0.82f, 0.28f, 0.68f, 0.34f),

                ["room_small"] = R(0.70f, 0.30f, 0.70f, 0.32f, 0.62f, 0.36f, 0.60f, 0.40f),
                ["room_medium"] = R(1.10f, 0.40f, 0.62f, 0.42f, 0.70f, 0.30f, 0.62f, 0.34f),
                ["room_large"] = R(1.80f, 0.50f, 0.54f, 0.54f, 0.76f, 0.26f, 0.68f, 0.30f),
                ["studio_dry"] = R(0.35f, 0.12f, 0.78f, 0.14f, 0.40f, 0.50f, 0.40f, 0.70f),
                ["studio_live"] = R(0.90f, 0.34f, 0.66f, 0.38f, 0.66f, 0.34f, 0.58f, 0.44f),
                ["broadcast_booth"] = R(0.28f, 0.08f, 0.82f, 0.10f, 0.30f, 0.58f, 0.45f, 0.70f),

                ["church_small"] = R(2.40f, 0.56f, 0.46f, 0.60f, 0.82f, 0.26f, 0.72f, 0.30f),
                ["church_large"] = R(3.80f, 0.70f, 0.40f, 0.76f, 0.86f, 0.22f, 0.78f, 0.24f),
                ["cathedral"] = R(5.40f, 0.78f, 0.34f, 0.84f, 0.90f, 0.20f, 0.82f, 0.20f),
                ["cave_small"] = R(2.60f, 0.62f, 0.46f, 0.66f, 0.74f, 0.20f, 0.86f, 0.24f),
                ["cave_large"] = R(4.50f, 0.78f, 0.34f, 0.84f, 0.84f, 0.16f, 0.92f, 0.16f),
                ["cave_ice"] = R(3.80f, 0.72f, 0.40f, 0.80f, 0.80f, 0.24f, 0.86f, 0.22f),
                ["canyon_narrow"] = R(2.10f, 0.54f, 0.46f, 0.58f, 0.52f, 0.34f, 0.64f, 0.42f),
                ["canyon_wide"] = R(2.90f, 0.60f, 0.44f, 0.64f, 0.48f, 0.36f, 0.56f, 0.48f),
                ["sewer_brick"] = R(2.10f, 0.62f, 0.50f, 0.68f, 0.78f, 0.22f, 0.86f, 0.22f),
                ["sewer_concrete"] = R(2.40f, 0.68f, 0.46f, 0.74f, 0.82f, 0.20f, 0.88f, 0.20f)
            };

        private static RoomValues R(
            float reverbTimeSeconds,
            float reverbGain,
            float hfDecayRatio,
            float lateReverbGain,
            float diffusion,
            float airAbsorption,
            float occlusionScale,
            float transmissionScale)
        {
            return new RoomValues
            {
                ReverbTimeSeconds = reverbTimeSeconds,
                ReverbGain = reverbGain,
                HfDecayRatio = hfDecayRatio,
                LateReverbGain = lateReverbGain,
                Diffusion = diffusion,
                AirAbsorption = airAbsorption,
                OcclusionScale = occlusionScale,
                TransmissionScale = transmissionScale
            };
        }

        public static bool IsPreset(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            return Presets.ContainsKey(name.Trim());
        }

        public static bool TryGetPreset(string name, out TrackRoomDefinition room)
        {
            room = null!;
            if (string.IsNullOrWhiteSpace(name))
                return false;
            if (!Presets.TryGetValue(name.Trim(), out var values))
                return false;

            var id = name.Trim();
            room = new TrackRoomDefinition(
                id,
                id,
                values.ReverbTimeSeconds,
                values.ReverbGain,
                values.HfDecayRatio,
                values.LateReverbGain,
                values.Diffusion,
                values.AirAbsorption,
                values.OcclusionScale,
                values.TransmissionScale);
            return true;
        }
    }
}
