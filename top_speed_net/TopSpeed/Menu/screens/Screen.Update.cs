using System;
using System.Collections.Generic;
using SharpDX.DirectInput;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        public MenuUpdateResult Update(InputManager input)
        {
            if (_items.Count == 0)
                return MenuUpdateResult.None;

            if (_titlePending)
            {
                if (input.IsAnyMenuInputHeld())
                    return MenuUpdateResult.None;
                _titlePending = false;
                AnnounceTitle();
            }

            var moveUp = input.WasPressed(Key.Up);
            var moveDown = input.WasPressed(Key.Down);
            var moveHome = input.WasPressed(Key.Home);
            var moveEnd = input.WasPressed(Key.End);
            var moveLeft = input.WasPressed(Key.Left);
            var moveRight = input.WasPressed(Key.Right);
            var pageUp = input.WasPressed(Key.PageUp);
            var pageDown = input.WasPressed(Key.PageDown);
            var activate = input.WasPressed(Key.Return) || input.WasPressed(Key.NumberPadEnter);
            var back = input.WasPressed(Key.Escape);

            if (input.TryGetJoystickState(out var joystick))
            {
                if (!_hasJoystickCenter && MenuInputUtil.IsNearCenter(joystick))
                {
                    _joystickCenter = joystick;
                    _hasJoystickCenter = true;
                }

                var previous = _hasPrevJoystick ? _prevJoystick : _joystickCenter;
                moveUp |= MenuInputUtil.WasJoystickUpPressed(joystick, previous);
                moveDown |= MenuInputUtil.WasJoystickDownPressed(joystick, previous);
                activate |= MenuInputUtil.WasJoystickActivatePressed(joystick, previous);
                back |= MenuInputUtil.WasJoystickBackPressed(joystick, previous);
                _prevJoystick = joystick;
                _hasPrevJoystick = true;
            }
            else
            {
                _hasPrevJoystick = false;
            }

            if (input.ShouldIgnoreMenuBack())
                return MenuUpdateResult.None;

            if (TryHandleShortcut(input))
                return MenuUpdateResult.None;

            if (TryHandleLetterNavigation(input))
                return MenuUpdateResult.None;

            if (_ignoreHeldInput)
            {
                if (input.IsMenuBackHeld())
                {
                    input.LatchMenuBack();
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    return MenuUpdateResult.Back;
                }
                if (moveUp)
                {
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    MoveToIndex(_items.Count - 1);
                    return MenuUpdateResult.None;
                }
                if (moveDown)
                {
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    MoveToIndex(0);
                    return MenuUpdateResult.None;
                }
                if (moveHome)
                {
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    MoveToIndex(0);
                    return MenuUpdateResult.None;
                }
                if (moveEnd)
                {
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    MoveToIndex(_items.Count - 1);
                    return MenuUpdateResult.None;
                }
                if (activate || back)
                {
                    _ignoreHeldInput = false;
                }
                else if (input.IsAnyMenuInputHeld())
                {
                    return MenuUpdateResult.None;
                }
                else
                {
                    _ignoreHeldInput = false;
                    input.ResetState();
                }
            }

            if (_index != NoSelection)
            {
                var adjustment = GetAdjustmentAction(moveLeft, moveRight, pageUp, pageDown, moveHome, moveEnd);
                if (adjustment.HasValue)
                {
                    var item = _items[_index];
                    if (item.Adjust(adjustment.Value, out var announcement))
                    {
                        PlayNavigateSound();
                        var safeAnnouncement = announcement;
                        if (!string.IsNullOrWhiteSpace(safeAnnouncement))
                        {
                            _speech.Speak(safeAnnouncement!);
                            CancelHint();
                        }
                        return MenuUpdateResult.None;
                    }
                }
            }

            if (_index != NoSelection)
            {
                var currentItem = _items[_index];
                if (currentItem.HasActions)
                {
                    if (moveRight && TryBrowseItemActions(currentItem, +1))
                        return MenuUpdateResult.None;

                    if (moveLeft && _activeActionIndex != NoSelection && TryBrowseItemActions(currentItem, -1))
                        return MenuUpdateResult.None;
                }
                else
                {
                    _activeActionIndex = NoSelection;
                }
            }

            if (_index == NoSelection)
            {
                if (moveDown)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(0);
                    _autoFocusPending = false;
                }
                else if (moveUp)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(_items.Count - 1);
                    _autoFocusPending = false;
                }
                else if (moveHome)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(0);
                    _autoFocusPending = false;
                }
                else if (moveEnd)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(_items.Count - 1);
                    _autoFocusPending = false;
                }
            }
            else
            {
                if (moveUp)
                {
                    _activeActionIndex = NoSelection;
                    MoveSelectionAndAnnounce(-1);
                }
                else if (moveDown)
                {
                    _activeActionIndex = NoSelection;
                    MoveSelectionAndAnnounce(1);
                }
                else if (moveHome)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(0);
                }
                else if (moveEnd)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(_items.Count - 1);
                }
            }

            if (pageUp)
            {
                SetMusicVolume(_musicVolume + 0.05f);
            }
            else if (pageDown)
            {
                SetMusicVolume(_musicVolume - 0.05f);
            }

            if (activate)
            {
                if (_index == NoSelection)
                    return MenuUpdateResult.None;
                if (_activeActionIndex != NoSelection)
                {
                    var item = _items[_index];
                    if (item.TryActivateAction(_activeActionIndex))
                    {
                        PlaySfx(_activateSound);
                        CancelHint();
                        return MenuUpdateResult.None;
                    }
                }
                PlaySfx(_activateSound);
                CancelHint();
                return MenuUpdateResult.Activated(_items[_index]);
            }

            if (back)
            {
                input.LatchMenuBack();
                return MenuUpdateResult.Back;
            }

            if (_index == NoSelection && _autoFocusPending)
            {
                FocusFirstItem();
                _autoFocusPending = false;
            }

            return MenuUpdateResult.None;
        }

        private bool TryHandleShortcut(InputManager input)
        {
            if (TryHandleShortcutList(_shortcuts, input))
                return true;
            return TryHandleShortcutList(_sharedShortcuts, input);
        }

        private bool TryHandleShortcutList(IReadOnlyList<MenuShortcut> shortcuts, InputManager input)
        {
            if (shortcuts == null || shortcuts.Count == 0)
                return false;

            for (var i = 0; i < shortcuts.Count; i++)
            {
                var shortcut = shortcuts[i];
                if (shortcut == null)
                    continue;
                if (!input.WasPressed(shortcut.Key))
                    continue;

                CancelHint();
                shortcut.OnTrigger();
                return true;
            }

            return false;
        }

        private bool TryHandleLetterNavigation(InputManager input)
        {
            if (_items.Count == 0)
                return false;

            if (!MenuInputUtil.TryGetPressedLetter(input, out var letter))
                return false;

            var start = _index == NoSelection ? 0 : (_index + 1) % _items.Count;
            for (var i = 0; i < _items.Count; i++)
            {
                var idx = (start + i) % _items.Count;
                if (!MenuInputUtil.ItemStartsWithLetter(_items[idx], letter))
                    continue;

                _activeActionIndex = NoSelection;
                MoveToIndex(idx);
                return true;
            }

            return false;
        }

        public void ResetSelection(int? preferredSelectionIndex = null)
        {
            _index = NoSelection;
            _activeActionIndex = NoSelection;
            _pendingFocusIndex = preferredSelectionIndex;
            _justEntered = true;
            _autoFocusPending = true;
            CancelHint();
        }

        public void ReplaceItems(IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            var previousIndex = _index;
            var hadSelection = previousIndex != NoSelection;

            _items.Clear();
            AddVisibleItems(_items, items);
            CancelHint();

            if (preserveSelection && hadSelection && _items.Count > 0)
            {
                _index = Math.Max(0, Math.Min(previousIndex, _items.Count - 1));
                _activeActionIndex = NoSelection;
                _pendingFocusIndex = null;
                _justEntered = false;
                _autoFocusPending = false;
                return;
            }

            _index = NoSelection;
            _activeActionIndex = NoSelection;
            _pendingFocusIndex = null;
            _justEntered = true;
            _autoFocusPending = true;
        }

        private static void AddVisibleItems(List<MenuItem> target, IEnumerable<MenuItem> items)
        {
            if (target == null || items == null)
                return;

            foreach (var item in items)
            {
                if (item == null || item.IsHidden)
                    continue;
                target.Add(item);
            }
        }

        private void MoveSelectionAndAnnounce(int delta)
        {
            var moved = MoveSelection(delta, out var wrapped, out var edgeReached);
            if (moved)
            {
                if (wrapped)
                {
                    PlayNavigateSound();
                    PlaySfx(_wrapSound);
                }
                else
                {
                    PlayNavigateSound();
                }
                AnnounceCurrent(!_justEntered);
                _justEntered = false;
            }
            else if (wrapped)
            {
                PlaySfx(_wrapSound);
            }
            else if (edgeReached)
            {
                PlaySfx(_edgeSound);
            }
        }

        private void MoveToIndex(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _items.Count)
                return;
            if (_index == NoSelection)
            {
                _index = targetIndex;
                PlayNavigateSound();
                AnnounceCurrent(!_justEntered);
                _justEntered = false;
                return;
            }
            if (targetIndex == _index)
            {
                PlaySfx(WrapNavigation ? _wrapSound : _edgeSound);
                return;
            }
            _index = targetIndex;
            PlayNavigateSound();
            AnnounceCurrent(!_justEntered);
            _justEntered = false;
        }

        private bool MoveSelection(int delta, out bool wrapped, out bool edgeReached)
        {
            wrapped = false;
            edgeReached = false;
            if (_items.Count == 0)
                return false;
            if (_index == NoSelection)
            {
                _index = delta >= 0 ? 0 : _items.Count - 1;
                return true;
            }
            var previous = _index;
            if (WrapNavigation)
            {
                var next = _index + delta;
                if (next < 0 || next >= _items.Count)
                    wrapped = true;
                _index = (next + _items.Count) % _items.Count;
                return _index != previous;
            }

            var nextIndex = _index + delta;
            if (nextIndex < 0 || nextIndex >= _items.Count)
            {
                edgeReached = true;
                return false;
            }
            _index = nextIndex;
            return _index != previous;
        }

        private void FocusFirstItem()
        {
            if (_items.Count == 0)
                return;
            var targetIndex = 0;
            if (_pendingFocusIndex.HasValue)
                targetIndex = Math.Max(0, Math.Min(_items.Count - 1, _pendingFocusIndex.Value));
            _pendingFocusIndex = null;
            _index = targetIndex;
            _activeActionIndex = NoSelection;
            PlayNavigateSound();
            AnnounceCurrent(purge: false);
            _justEntered = false;
        }

        private bool TryBrowseItemActions(MenuItem item, int direction)
        {
            if (!item.HasActions || item.ActionCount <= 0)
                return false;

            var nextIndex = _activeActionIndex == NoSelection
                ? (direction >= 0 ? 0 : item.ActionCount - 1)
                : _activeActionIndex + direction;

            if (WrapNavigation)
            {
                nextIndex = (nextIndex % item.ActionCount + item.ActionCount) % item.ActionCount;
            }
            else if (nextIndex < 0 || nextIndex >= item.ActionCount)
            {
                PlaySfx(_edgeSound);
                return true;
            }

            _activeActionIndex = nextIndex;
            if (item.TryGetActionLabel(_activeActionIndex, out var label) && !string.IsNullOrWhiteSpace(label))
            {
                PlayNavigateSound();
                _speech.Speak(label);
                CancelHint();
            }
            else
            {
                PlayNavigateSound();
            }

            return true;
        }

        private static MenuAdjustAction? GetAdjustmentAction(bool moveLeft, bool moveRight, bool pageUp, bool pageDown, bool moveHome, bool moveEnd)
        {
            if (moveLeft)
                return MenuAdjustAction.Decrease;
            if (moveRight)
                return MenuAdjustAction.Increase;
            if (pageUp)
                return MenuAdjustAction.PageIncrease;
            if (pageDown)
                return MenuAdjustAction.PageDecrease;
            if (moveHome)
                return MenuAdjustAction.ToMaximum;
            if (moveEnd)
                return MenuAdjustAction.ToMinimum;
            return null;
        }
    }
}
