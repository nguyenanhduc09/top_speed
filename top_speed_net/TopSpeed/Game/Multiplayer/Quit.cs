using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void OpenMultiplayerRaceQuitConfirmation()
        {
            if (_multiplayerRace == null)
                return;
            if (_multiplayerRaceQuitConfirmActive)
                return;
            if (_multiplayerCoordinator.Questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _multiplayerRaceQuitConfirmActive = true;

            var question = new Question(LocalizationService.Mark("Quit race?"),
                LocalizationService.Mark("Are you sure you want to quit this multiplayer race?"),
                QuestionId.No,
                HandleMultiplayerRaceQuitQuestionResult,
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, quit the race")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No, continue racing"), flags: QuestionButtonFlags.Default))
            {
                OpenAsOverlay = true
            };
            _multiplayerCoordinator.Questions.Show(question);
        }

        private void HandleMultiplayerRaceQuitQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                ConfirmQuitMultiplayerRace();
            else if (resultId == QuestionId.No || resultId == QuestionId.Cancel || resultId == QuestionId.Close)
                CancelMultiplayerRaceQuitConfirmation();
        }

        private void CancelMultiplayerRaceQuitConfirmation()
        {
            if (!_multiplayerRaceQuitConfirmActive)
                return;

            _multiplayerRaceQuitConfirmActive = false;
        }

        private void ConfirmQuitMultiplayerRace()
        {
            if (!_multiplayerRaceQuitConfirmActive)
                return;

            _multiplayerRaceQuitConfirmActive = false;
            if (_session != null)
                TrySendSession(_session.SendRoomLeave());

            _multiplayerRace?.FinalizeMultiplayerMode();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;

            ResetPendingMultiplayerState();
            _state = AppState.Menu;
            _menu.ShowRoot("multiplayer_lobby");
        }

        private bool TrySendSession(bool sent)
        {
            if (sent)
                return true;

            _speech.Speak(LocalizationService.Mark("Failed to send data. Please check your connection."));
            return false;
        }
    }
}





