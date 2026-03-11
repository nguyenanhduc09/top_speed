using System;
using System.Collections.Generic;
using TopSpeed.Menu;
using TopSpeed.Speech;
using TopSpeed.Windowing;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenGlobalChatInput()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            _promptTextInput(
                "Enter your global chat message.",
                null,
                SpeechService.SpeakFlag.None,
                true,
                HandleGlobalChatInput);
        }

        private void OpenRoomChatInput()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom)
            {
                _speech.Speak("You are not in a game room.");
                return;
            }

            _promptTextInput(
                "Enter your room chat message.",
                null,
                SpeechService.SpeakFlag.None,
                true,
                HandleRoomChatInput);
        }

        private void HandleGlobalChatInput(TextInputResult result)
        {
            if (result.Cancelled)
                return;

            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            var text = (result.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _speech.Speak("Chat message cannot be empty.");
                return;
            }

            if (!session.SendChatMessage(text))
                _speech.Speak("Failed to send chat message.");
        }

        private void HandleRoomChatInput(TextInputResult result)
        {
            if (result.Cancelled)
                return;

            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom)
            {
                _speech.Speak("You are not in a game room.");
                return;
            }

            var text = (result.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _speech.Speak("Chat message cannot be empty.");
                return;
            }

            if (!session.SendRoomChatMessage(text))
                _speech.Speak("Failed to send room chat message.");
        }

        private void AddGlobalChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _historyBuffers.AddGlobalChat(text);
            UpdateHistoryScreens();
        }

        private void AddRoomChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _historyBuffers.AddRoomChat(text);
            UpdateHistoryScreens();
        }

        private void AddConnectionMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _historyBuffers.AddConnection(text);
            UpdateHistoryScreens();
        }

        private void AddRoomEventMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _historyBuffers.AddRoomEvent(text);
            UpdateHistoryScreens();
        }

        internal void NextChatCategory()
        {
            _historyBuffers.MoveToNext();
            PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            _speech.Speak(_historyBuffers.CategoryLabel(), SpeechService.SpeakFlag.None);
        }

        internal void PreviousChatCategory()
        {
            _historyBuffers.MoveToPrevious();
            PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            _speech.Speak(_historyBuffers.CategoryLabel(), SpeechService.SpeakFlag.None);
        }

        internal void OpenGlobalChatHotkey()
        {
            OpenGlobalChatInput();
        }

        internal void OpenRoomChatHotkey()
        {
            OpenRoomChatInput();
        }

        private void UpdateHistoryScreens()
        {
            var items = _historyBuffers.GetCurrentItems();
            TryUpdateChatScreen(MultiplayerLobbyMenuId, items);
            TryUpdateChatScreen(MultiplayerRoomControlsMenuId, items);
        }

        private void TryUpdateChatScreen(string menuId, IEnumerable<MenuItem> items)
        {
            try
            {
                _menu.UpdateItems(menuId, SharedLobbyChatScreenId, items, preserveSelection: true);
            }
            catch (InvalidOperationException)
            {
                // Menus may not be registered yet during startup.
            }
        }

        private static string? NormalizeChatMessage(string message)
        {
            var text = (message ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
