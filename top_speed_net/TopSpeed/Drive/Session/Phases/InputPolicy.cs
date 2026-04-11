namespace TopSpeed.Drive.Session
{
    internal sealed class InputPolicy
    {
        public bool AllowDrivingInput { get; set; }
        public bool AllowAuxiliaryInput { get; set; }
        public bool AllowHorn { get; set; }

        public static InputPolicy Create(bool allowDrivingInput, bool allowAuxiliaryInput, bool allowHorn)
        {
            return new InputPolicy
            {
                AllowDrivingInput = allowDrivingInput,
                AllowAuxiliaryInput = allowAuxiliaryInput,
                AllowHorn = allowHorn
            };
        }
    }
}
