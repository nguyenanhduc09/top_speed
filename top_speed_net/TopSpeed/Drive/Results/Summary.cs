using System;

namespace TopSpeed.Drive
{
    internal enum DriveResultMode
    {
        Race = 0,
        TimeTrial = 1
    }

    internal sealed class DriveResultSummary
    {
        public DriveResultMode Mode { get; set; } = DriveResultMode.Race;
        public bool IsMultiplayer { get; set; }
        public int LocalPosition { get; set; }
        public int LocalCrashCount { get; set; }
        public bool TimeTrialBeatRecord { get; set; }
        public int TimeTrialLapCount { get; set; }
        public int TimeTrialCurrentRunMs { get; set; }
        public int TimeTrialBestRunMs { get; set; }
        public int TimeTrialAverageRunMs { get; set; }
        public int TimeTrialBestLapThisRunMs { get; set; }
        public int TimeTrialBestLapMs { get; set; }
        public int TimeTrialAverageLapMs { get; set; }
        public DriveResultEntry[] Entries { get; set; } = Array.Empty<DriveResultEntry>();
    }

    internal sealed class DriveResultEntry
    {
        public string Name { get; set; } = string.Empty;
        public int Position { get; set; }
        public int TimeMs { get; set; }
        public bool IsLocalPlayer { get; set; }
    }
}


