namespace TopSpeed.Core
{
    internal enum DriveMode
    {
        QuickStart,
        TimeTrial,
        SingleRace
    }

    internal enum TrackCategory
    {
        RaceTrack,
        StreetAdventure,
        CustomTrack
    }

    internal enum TransmissionMode
    {
        Automatic,
        Manual
    }

    internal sealed class DriveSetup
    {
        public DriveMode Mode { get; set; } = DriveMode.QuickStart;
        public TrackCategory TrackCategory { get; set; } = TrackCategory.RaceTrack;
        public string? TrackNameOrFile { get; set; }
        public int? VehicleIndex { get; set; }
        public string? VehicleFile { get; set; }
        public TransmissionMode Transmission { get; set; } = TransmissionMode.Automatic;

        public void ClearSelection()
        {
            TrackNameOrFile = null;
            VehicleIndex = null;
            VehicleFile = null;
            Transmission = TransmissionMode.Automatic;
        }
    }
}



