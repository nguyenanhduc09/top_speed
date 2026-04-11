using System;
using TopSpeed.Input;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class PlayerInfo : Subsystem
    {
        private readonly DriveInput _input;
        private readonly Func<int> _getMaxPlayerIndex;
        private readonly Func<int, bool> _hasPlayer;
        private readonly Func<int, string> _getVehicleName;
        private readonly Func<bool> _isStarted;
        private readonly Func<int, int>? _getPlayerPercent;
        private readonly Action<string> _speakText;
        private readonly Action? _updateExtra;

        public PlayerInfo(
            string name,
            int order,
            DriveInput input,
            Func<int> getMaxPlayerIndex,
            Func<int, bool> hasPlayer,
            Func<int, string> getVehicleName,
            Func<bool> isStarted,
            Action<string> speakText,
            Func<int, int>? getPlayerPercent = null,
            Action? updateExtra = null)
            : base(name, order)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _getMaxPlayerIndex = getMaxPlayerIndex ?? throw new ArgumentNullException(nameof(getMaxPlayerIndex));
            _hasPlayer = hasPlayer ?? throw new ArgumentNullException(nameof(hasPlayer));
            _getVehicleName = getVehicleName ?? throw new ArgumentNullException(nameof(getVehicleName));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _speakText = speakText ?? throw new ArgumentNullException(nameof(speakText));
            _getPlayerPercent = getPlayerPercent;
            _updateExtra = updateExtra;
        }

        public override void Update(SessionContext context, float elapsed)
        {
            _updateExtra?.Invoke();

            var maxPlayerIndex = _getMaxPlayerIndex();
            if (_input.TryGetPlayerInfo(out var infoPlayer)
                && infoPlayer >= 0
                && infoPlayer <= maxPlayerIndex
                && _hasPlayer(infoPlayer))
            {
                _speakText(_getVehicleName(infoPlayer));
            }

            if (_getPlayerPercent == null || !_isStarted())
                return;

            if (_input.TryGetPlayerPosition(out var positionPlayer)
                && positionPlayer >= 0
                && positionPlayer <= maxPlayerIndex
                && _hasPlayer(positionPlayer))
            {
                _speakText(SessionText.FormatPlayerPercentage(_getPlayerPercent(positionPlayer)));
            }
        }
    }
}
