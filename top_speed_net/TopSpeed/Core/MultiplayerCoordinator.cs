using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Speech;
using TopSpeed.Windowing;

namespace TopSpeed.Core
{
    internal sealed class MultiplayerCoordinator
    {
        private readonly MenuManager _menu;
        private readonly SpeechService _speech;
        private readonly RaceSettings _settings;
        private readonly MultiplayerConnector _connector;
        private readonly Func<string, string?, SpeechService.SpeakFlag, bool, TextInputResult> _promptTextInput;
        private readonly Action _saveSettings;
        private readonly Action _enterMenuState;
        private readonly Action<MultiplayerSession> _setSession;
        private readonly Func<MultiplayerSession?> _getSession;
        private readonly Action _clearSession;
        private readonly Action _resetPendingState;

        private Task<IReadOnlyList<ServerInfo>>? _discoveryTask;
        private CancellationTokenSource? _discoveryCts;
        private Task<ConnectResult>? _connectTask;
        private CancellationTokenSource? _connectCts;
        private string _pendingServerAddress = string.Empty;
        private int _pendingServerPort;
        private string _pendingCallSign = string.Empty;

        private PacketRoomList _roomList = new PacketRoomList();
        private PacketRoomState _roomState = new PacketRoomState { InRoom = false, Players = Array.Empty<PacketRoomPlayer>() };
        private bool _wasInRoom;
        private uint _lastRoomId;
        private bool _wasHost;

        public MultiplayerCoordinator(
            MenuManager menu,
            SpeechService speech,
            RaceSettings settings,
            MultiplayerConnector connector,
            Func<string, string?, SpeechService.SpeakFlag, bool, TextInputResult> promptTextInput,
            Action saveSettings,
            Action enterMenuState,
            Action<MultiplayerSession> setSession,
            Func<MultiplayerSession?> getSession,
            Action clearSession,
            Action resetPendingState)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            _promptTextInput = promptTextInput ?? throw new ArgumentNullException(nameof(promptTextInput));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
            _enterMenuState = enterMenuState ?? throw new ArgumentNullException(nameof(enterMenuState));
            _setSession = setSession ?? throw new ArgumentNullException(nameof(setSession));
            _getSession = getSession ?? throw new ArgumentNullException(nameof(getSession));
            _clearSession = clearSession ?? throw new ArgumentNullException(nameof(clearSession));
            _resetPendingState = resetPendingState ?? throw new ArgumentNullException(nameof(resetPendingState));
        }

        public bool UpdatePendingOperations()
        {
            if (_connectTask != null)
            {
                if (!_connectTask.IsCompleted)
                    return true;

                var result = _connectTask.IsFaulted || _connectTask.IsCanceled
                    ? ConnectResult.CreateFail("Connection attempt failed.")
                    : _connectTask.GetAwaiter().GetResult();
                _connectTask = null;
                _connectCts?.Dispose();
                _connectCts = null;
                HandleConnectResult(result);
                return false;
            }

            if (_discoveryTask != null)
            {
                if (!_discoveryTask.IsCompleted)
                    return true;

                IReadOnlyList<ServerInfo> servers;
                if (_discoveryTask.IsFaulted || _discoveryTask.IsCanceled)
                    servers = Array.Empty<ServerInfo>();
                else
                    servers = _discoveryTask.GetAwaiter().GetResult();

                _discoveryTask = null;
                _discoveryCts?.Dispose();
                _discoveryCts = null;
                HandleDiscoveryResult(servers);
                return false;
            }

            return false;
        }

        public void OnSessionCleared()
        {
            _roomList = new PacketRoomList();
            _roomState = new PacketRoomState { InRoom = false, Players = Array.Empty<PacketRoomPlayer>() };
            _wasInRoom = false;
            _wasHost = false;
            _lastRoomId = 0;
            RebuildLobbyMenu();
            RebuildRoomControlsMenu();
            RebuildRoomOptionsMenu();
            UpdateRoomBrowserMenu();
        }

        public void HandleRoomList(PacketRoomList roomList)
        {
            _roomList = roomList ?? new PacketRoomList();
            UpdateRoomBrowserMenu();
        }

        public void HandleRoomState(PacketRoomState roomState)
        {
            _roomState = roomState ?? new PacketRoomState { InRoom = false, Players = Array.Empty<PacketRoomPlayer>() };

            if (_roomState.InRoom)
            {
                if (!_wasInRoom || _lastRoomId != _roomState.RoomId)
                {
                    var roomName = string.IsNullOrWhiteSpace(_roomState.RoomName) ? "game room" : _roomState.RoomName;
                    _speech.Speak($"Joined {roomName}.");
                }

                if (_roomState.IsHost && (!_wasHost || !_wasInRoom))
                    _speech.Speak("You are now host of this game.");
            }
            else if (_wasInRoom)
            {
                _speech.Speak("You left the game room.");
            }

            _wasInRoom = _roomState.InRoom;
            _lastRoomId = _roomState.RoomId;
            _wasHost = _roomState.IsHost;

            RebuildLobbyMenu();
            RebuildRoomControlsMenu();
            RebuildRoomOptionsMenu();
            UpdateRoomBrowserMenu();
        }

        public void HandleProtocolMessage(PacketProtocolMessage message)
        {
            if (message == null)
                return;

            if (!string.IsNullOrWhiteSpace(message.Message))
                _speech.Speak(message.Message);
        }

        public void StartServerDiscovery()
        {
            if (_discoveryTask != null && !_discoveryTask.IsCompleted)
                return;

            _speech.Speak("Please wait. Scanning for servers on the local network.");
            _discoveryCts?.Cancel();
            _discoveryCts?.Dispose();
            _discoveryCts = new CancellationTokenSource();
            _discoveryTask = Task.Run(async () =>
            {
                using var client = new DiscoveryClient();
                return await client.ScanAsync(ClientProtocol.DefaultDiscoveryPort, TimeSpan.FromSeconds(2), _discoveryCts.Token);
            }, _discoveryCts.Token);
        }

        public void BeginManualServerEntry()
        {
            while (true)
            {
                var result = _promptTextInput("Enter the server IP address or domain.", _settings.LastServerAddress,
                    SpeechService.SpeakFlag.InterruptableButStop, true);
                if (result.Cancelled)
                    return;
                if (HandleServerAddressInput(result.Text))
                    return;
            }
        }

        public void BeginServerPortEntry()
        {
            var current = _settings.ServerPort > 0 ? _settings.ServerPort.ToString() : string.Empty;
            var result = _promptTextInput("Enter a custom server port, or leave empty for default.", current,
                SpeechService.SpeakFlag.None, true);
            if (result.Cancelled)
                return;
            HandleServerPortInput(result.Text);
        }

        private void HandleDiscoveryResult(IReadOnlyList<ServerInfo> servers)
        {
            if (servers == null || servers.Count == 0)
            {
                _speech.Speak("No servers were found on the local network. You can enter an address manually.");
                return;
            }

            var items = new List<MenuItem>();
            foreach (var server in servers)
            {
                var info = server;
                var label = $"{info.Address}:{info.Port}";
                items.Add(new MenuItem(label, MenuAction.None, onActivate: () => SelectDiscoveredServer(info), suppressPostActivateAnnouncement: true));
            }

            items.Add(new MenuItem("Go back", MenuAction.Back));
            _menu.UpdateItems("multiplayer_servers", items);
            _menu.Push("multiplayer_servers");
        }

        private void SelectDiscoveredServer(ServerInfo server)
        {
            _pendingServerAddress = server.Address.ToString();
            _pendingServerPort = server.Port;
            BeginCallSignInput();
        }

        private bool HandleServerAddressInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _speech.Speak("Please enter a server address.");
                return false;
            }

            var host = trimmed;
            int? overridePort = null;
            var lastColon = trimmed.LastIndexOf(':');
            if (lastColon > 0 && lastColon < trimmed.Length - 1)
            {
                var portPart = trimmed.Substring(lastColon + 1);
                if (int.TryParse(portPart, out var parsedPort))
                {
                    host = trimmed.Substring(0, lastColon);
                    overridePort = parsedPort;
                }
            }

            _settings.LastServerAddress = host;
            _saveSettings();
            _pendingServerAddress = host;
            _pendingServerPort = overridePort ?? ResolveServerPort();
            return BeginCallSignInput();
        }

        private bool BeginCallSignInput()
        {
            while (true)
            {
                var result = _promptTextInput("Enter your call sign.", null,
                    SpeechService.SpeakFlag.InterruptableButStop, true);
                if (result.Cancelled)
                    return false;
                if (HandleCallSignInput(result.Text))
                    return true;
            }
        }

        private bool HandleCallSignInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _speech.Speak("Call sign cannot be empty.");
                return false;
            }

            _pendingCallSign = trimmed;
            AttemptConnect(_pendingServerAddress, _pendingServerPort, _pendingCallSign);
            return true;
        }

        private void AttemptConnect(string host, int port, string callSign)
        {
            _speech.Speak("Attempting to connect, please wait...");
            _clearSession();
            _connectCts?.Cancel();
            _connectCts?.Dispose();
            _connectCts = new CancellationTokenSource();
            _connectTask = _connector.ConnectAsync(host, port, callSign, TimeSpan.FromSeconds(3), _connectCts.Token);
        }

        private void HandleConnectResult(ConnectResult result)
        {
            if (result.Success && result.Session != null)
            {
                var session = result.Session;
                _setSession(session);
                _resetPendingState();

                OnSessionCleared();
                session.SendRoomListRequest();

                var welcome = "Connected to server.";
                if (!string.IsNullOrWhiteSpace(result.Motd))
                    welcome += $" Message of the day: {result.Motd}.";
                _speech.Speak(welcome);
                _menu.ShowRoot("multiplayer_lobby");
                _enterMenuState();
                return;
            }

            _speech.Speak($"Failed to connect: {result.Message}");
            _enterMenuState();
        }

        private void HandleServerPortInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                _settings.ServerPort = 0;
                _saveSettings();
                _speech.Speak("Server port cleared. The default port will be used.");
                return;
            }

            if (!int.TryParse(trimmed, out var port) || port < 1 || port > 65535)
            {
                _speech.Speak("Invalid port. Enter a number between 1 and 65535.");
                BeginServerPortEntry();
                return;
            }

            _settings.ServerPort = port;
            _saveSettings();
            _speech.Speak($"Server port set to {port}.");
        }

        private int ResolveServerPort()
        {
            return _settings.ServerPort > 0 ? _settings.ServerPort : ClientProtocol.DefaultServerPort;
        }

        private MultiplayerSession? SessionOrNull()
        {
            return _getSession();
        }

        private void RebuildLobbyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Create a new game", MenuAction.None, onActivate: CreateRoom),
                new MenuItem("Join an existing game", MenuAction.None, onActivate: OpenRoomBrowser),
                new MenuItem("Refresh game room list", MenuAction.None, onActivate: RequestRoomList)
            };

            if (_roomState.InRoom)
                items.Add(new MenuItem("Room controls", MenuAction.None, nextMenuId: "multiplayer_room_controls"));

            items.Add(new MenuItem("Options", MenuAction.None, nextMenuId: "options_main"));
            items.Add(new MenuItem("Disconnect", MenuAction.None, onActivate: Disconnect));
            _menu.UpdateItems("multiplayer_lobby", items);
        }

        private void RebuildRoomControlsMenu()
        {
            var items = new List<MenuItem>();
            if (!_roomState.InRoom)
            {
                items.Add(new MenuItem("Join a game room first", MenuAction.None));
                items.Add(new MenuItem("Go back", MenuAction.Back));
                _menu.UpdateItems("multiplayer_room_controls", items);
                return;
            }

            if (_roomState.IsHost)
                items.Add(new MenuItem("Start game", MenuAction.None, onActivate: StartGame));
            if (_roomState.IsHost)
                items.Add(new MenuItem("Change game options", MenuAction.None, nextMenuId: "multiplayer_room_options"));
            items.Add(new MenuItem("Who is present in this game", MenuAction.None, onActivate: SpeakPresentPlayers));
            items.Add(new MenuItem("Leave room", MenuAction.None, onActivate: LeaveRoom));
            items.Add(new MenuItem("Go back", MenuAction.Back));
            _menu.UpdateItems("multiplayer_room_controls", items);
        }

        private void RebuildRoomOptionsMenu()
        {
            var items = new List<MenuItem>();
            if (!_roomState.InRoom)
            {
                items.Add(new MenuItem("Join a game room first", MenuAction.None));
                items.Add(new MenuItem("Go back", MenuAction.Back));
                _menu.UpdateItems("multiplayer_room_options", items);
                return;
            }

            if (!_roomState.IsHost)
            {
                items.Add(new MenuItem("Only the host can change game options", MenuAction.None));
                items.Add(new MenuItem("Go back", MenuAction.Back));
                _menu.UpdateItems("multiplayer_room_options", items);
                return;
            }

            items.Add(new MenuItem("Set game track", MenuAction.None, onActivate: ChangeTrack));
            items.Add(new MenuItem("Set game laps", MenuAction.None, onActivate: ChangeLaps));

            var playerOptions = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
            items.Add(new RadioButton(
                "Players needed to start",
                playerOptions,
                () => Math.Max(0, Math.Min(playerOptions.Length - 1, (_roomState.PlayersToStart > 0 ? _roomState.PlayersToStart : (byte)1) - 1)),
                value => SetPlayersToStart((byte)(value + 1)),
                hint: "Select how many players are required before the host can start this game. Use LEFT or RIGHT to change."));

            items.Add(new MenuItem("Go back", MenuAction.Back));
            _menu.UpdateItems("multiplayer_room_options", items);
        }

        private void RequestRoomList()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            session.SendRoomListRequest();
            UpdateRoomBrowserMenu();
        }

        private void OpenRoomBrowser()
        {
            RequestRoomList();
            _menu.Push("multiplayer_rooms");
        }

        private void CreateRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            var typeResult = _promptTextInput(
                "Select game type. Enter 1 for race with bots, or 2 for one-on-one without bots.",
                "1",
                SpeechService.SpeakFlag.None,
                true);
            if (typeResult.Cancelled)
                return;

            var roomType = ParseRoomType(typeResult.Text, out var parsedType)
                ? parsedType
                : GameRoomType.BotsRace;

            byte playersToStart;
            if (roomType == GameRoomType.OneOnOne)
            {
                playersToStart = 2;
            }
            else
            {
                var playersResult = _promptTextInput(
                    "Enter how many players are needed to start this game room, from 1 to 10.",
                    "2",
                    SpeechService.SpeakFlag.None,
                    true);
                if (playersResult.Cancelled)
                    return;

                if (!byte.TryParse(playersResult.Text, out playersToStart) || playersToStart < 1 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                {
                    _speech.Speak("Invalid number of players. Using 2.");
                    playersToStart = 2;
                }
            }

            var nameResult = _promptTextInput("Enter a game room name, or leave empty for automatic name.", null, SpeechService.SpeakFlag.None, true);
            if (nameResult.Cancelled)
                return;

            session.SendRoomCreate(nameResult.Text, roomType, playersToStart);
        }

        private static bool ParseRoomType(string text, out GameRoomType roomType)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (trimmed == "2")
            {
                roomType = GameRoomType.OneOnOne;
                return true;
            }

            if (trimmed == "1")
            {
                roomType = GameRoomType.BotsRace;
                return true;
            }

            roomType = GameRoomType.BotsRace;
            return false;
        }

        private void JoinRoom(uint roomId)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            session.SendRoomJoin(roomId);
            _menu.ShowRoot("multiplayer_lobby");
        }

        private void LeaveRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            session.SendRoomLeave();
        }

        private void StartGame()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom || !_roomState.IsHost)
            {
                _speech.Speak("Only the host can start the game.");
                return;
            }

            session.SendRoomStartRace();
        }

        private void ChangeTrack()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom || !_roomState.IsHost)
            {
                _speech.Speak("Only the host can change game options.");
                return;
            }

            var current = string.IsNullOrWhiteSpace(_roomState.TrackName) ? "america" : _roomState.TrackName;
            var result = _promptTextInput("Enter track key, for example america or germany.", current, SpeechService.SpeakFlag.None, true);
            if (result.Cancelled)
                return;

            session.SendRoomSetTrack(result.Text);
        }

        private void ChangeLaps()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom || !_roomState.IsHost)
            {
                _speech.Speak("Only the host can change game options.");
                return;
            }

            var current = _roomState.Laps > 0 ? _roomState.Laps.ToString() : "3";
            var result = _promptTextInput("Enter number of laps between 1 and 16.", current, SpeechService.SpeakFlag.None, true);
            if (result.Cancelled)
                return;

            if (!byte.TryParse(result.Text, out var laps) || laps < 1 || laps > 16)
            {
                _speech.Speak("Invalid laps value.");
                return;
            }

            session.SendRoomSetLaps(laps);
        }

        private void SetPlayersToStart(byte playersToStart)
        {
            var session = SessionOrNull();
            if (session == null || !_roomState.IsHost || !_roomState.InRoom)
                return;

            session.SendRoomSetPlayersToStart(playersToStart);
        }

        private void SpeakPresentPlayers()
        {
            if (!_roomState.InRoom)
            {
                _speech.Speak("You are not in a game room.");
                return;
            }

            if (_roomState.Players == null || _roomState.Players.Length == 0)
            {
                _speech.Speak("No players are in this game.");
                return;
            }

            var parts = new List<string>();
            foreach (var player in _roomState.Players)
            {
                var name = string.IsNullOrWhiteSpace(player.Name) ? $"Player {player.PlayerNumber + 1}" : player.Name;
                if (player.PlayerId == _roomState.HostPlayerId)
                    parts.Add($"{name}, host");
                else
                    parts.Add(name);
            }

            _speech.Speak(string.Join(". ", parts));
        }

        private void Disconnect()
        {
            _clearSession();
            _speech.Speak("Disconnected from server.");
            _menu.ShowRoot("main");
            _enterMenuState();
        }

        private void UpdateRoomBrowserMenu()
        {
            var items = new List<MenuItem>();
            var rooms = _roomList.Rooms ?? Array.Empty<PacketRoomSummary>();
            if (rooms.Length == 0)
            {
                items.Add(new MenuItem("No game rooms found", MenuAction.None));
            }
            else
            {
                foreach (var room in rooms)
                {
                    var roomCopy = room;
                    var typeText = roomCopy.RoomType == GameRoomType.OneOnOne ? "one-on-one" : "bots";
                    var label = $"{typeText} game with {roomCopy.PlayerCount} people";
                    if (roomCopy.RaceStarted)
                        label += ", in progress";
                    items.Add(new MenuItem(label, MenuAction.None, onActivate: () => JoinRoom(roomCopy.RoomId)));
                }
            }

            items.Add(new MenuItem("Go back", MenuAction.Back));
            _menu.UpdateItems("multiplayer_rooms", items);
        }
    }
}
