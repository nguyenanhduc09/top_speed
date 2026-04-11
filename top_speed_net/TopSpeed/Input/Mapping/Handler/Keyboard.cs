using Key = TopSpeed.Input.InputKey;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed partial class InputMappingHandler
    {
        private void TryCaptureKeyboardMapping()
        {
            for (var i = 1; i < 256; i++)
            {
                var key = (Key)i;
                if (!_input.WasPressed(key))
                    continue;
                if (KeyMapManager.IsReservedKey(key))
                {
                    _speech.Speak(LocalizationService.Mark("That key is reserved."));
                    return;
                }
                if (_driveInput.KeyMap.IsKeyInUse(key, _mappingAction))
                {
                    _speech.Speak(LocalizationService.Mark("That key is already in use."));
                    return;
                }

                _driveInput.KeyMap.ApplyKeyMapping(_mappingAction, key);
                _saveSettings();
                _mappingActive = false;
                var label = _driveInput.KeyMap.GetLabel(_mappingAction);
                _speech.Speak(LocalizationService.Format(
                    LocalizationService.Mark("{0} set to {1}."),
                    label,
                    KeyMapManager.FormatKey(key)));
                return;
            }
        }
    }
}



