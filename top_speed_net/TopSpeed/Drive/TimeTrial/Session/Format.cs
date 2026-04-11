using System;

namespace TopSpeed.Drive.TimeTrial
{
    internal sealed partial class TimeTrialSession
    {
        private string GetVehicleName()
        {
            if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                return TopSpeed.Drive.Session.SessionText.FormatVehicleName(_car.CustomFile);

            return _car.VehicleName;
        }
    }
}
