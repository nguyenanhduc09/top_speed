namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public void Update(float deltaSeconds)
        {
            _input.Update();
            if (_input.TryGetControllerState(out var controller))
                _driveInput.Run(_input.Current, controller, deltaSeconds, _input.ActiveControllerIsRacingWheel);
            else
                _driveInput.Run(_input.Current, deltaSeconds);

            TryShowDeviceChoiceDialog();

            _driveInput.SetOverlayInputBlocked(
                _state == AppState.MultiplayerRace &&
                (_multiplayerCoordinator.Questions.HasActiveOverlayQuestion
                 || _dialogs.HasActiveOverlayDialog
                 || _choices.HasActiveChoiceDialog));

            UpdateTextInputPrompt();
            _stateMachine.Update(deltaSeconds);

            if (_pendingDriveStart)
            {
                _pendingDriveStart = false;
                StartDrive(_pendingMode);
            }

            SyncAudioLoopState();
        }
    }
}


