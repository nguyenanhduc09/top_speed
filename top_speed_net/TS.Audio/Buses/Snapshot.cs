using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioBusSnapshot
    {
        public string Name { get; }
        public string? ParentName { get; }
        public float LocalVolume { get; }
        public float EffectiveVolume { get; }
        public bool Muted { get; }
        public int ChildCount { get; }
        public bool EffectsEnabled { get; }
        public int EffectCount { get; }
        public IReadOnlyList<string> Effects { get; }

        public AudioBusSnapshot(string name, string? parentName, float localVolume, float effectiveVolume, bool muted, int childCount, bool effectsEnabled, int effectCount, IReadOnlyList<string> effects)
        {
            Name = name;
            ParentName = parentName;
            LocalVolume = localVolume;
            EffectiveVolume = effectiveVolume;
            Muted = muted;
            ChildCount = childCount;
            EffectsEnabled = effectsEnabled;
            EffectCount = effectCount;
            Effects = effects;
        }
    }
}
