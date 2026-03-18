using System;
using System.Collections.Generic;
using TopSpeed.Menu;
using TopSpeed.Protocol;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenRoomBrowser()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (_state.Rooms.IsRoomBrowserOpenPending)
                return;

            _state.Rooms.IsRoomBrowserOpenPending = true;
            if (!TrySend(session.SendRoomListRequest()))
                _state.Rooms.IsRoomBrowserOpenPending = false;
        }

        private void JoinRoom(uint roomId)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            TrySend(session.SendRoomJoin(roomId));
        }

        private void UpdateRoomBrowserMenu()
        {
            var items = new List<MenuItem>();
            var rooms = _state.Rooms.RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>();
            if (rooms.Length == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No game rooms found"), MenuAction.None));
            }
            else
            {
                foreach (var room in rooms)
                {
                    var roomCopy = room;
                    var typeText = roomCopy.RoomType switch
                    {
                        GameRoomType.OneOnOne => LocalizationService.Translate(LocalizationService.Mark("one-on-one")),
                        GameRoomType.PlayersRace => LocalizationService.Translate(LocalizationService.Mark("race without bots")),
                        _ => LocalizationService.Translate(LocalizationService.Mark("race with bots"))
                    };
                    var label = typeText;
                    if (!string.IsNullOrWhiteSpace(roomCopy.RoomName))
                        label = label + ", " + roomCopy.RoomName;
                    label = LocalizationService.Format(
                        LocalizationService.Mark("{0} game with {1} people"),
                        label,
                        roomCopy.PlayerCount);
                    label = LocalizationService.Format(
                        LocalizationService.Mark("{0}, maximum {1} players"),
                        label,
                        roomCopy.PlayersToStart);
                    if (roomCopy.RaceStarted)
                        label = LocalizationService.Format(LocalizationService.Mark("{0}, in progress"), label);
                    else if (roomCopy.PlayerCount >= roomCopy.PlayersToStart)
                        label = LocalizationService.Format(LocalizationService.Mark("{0}, room is full"), label);
                    items.Add(new MenuItem(label, MenuAction.None, onActivate: () => JoinRoom(roomCopy.RoomId)));
                }
            }

            items.Add(new MenuItem(LocalizationService.Mark("Return to multiplayer lobby"), MenuAction.Back));
            _menu.UpdateItems(MultiplayerMenuKeys.RoomBrowser, items);
        }
    }
}





