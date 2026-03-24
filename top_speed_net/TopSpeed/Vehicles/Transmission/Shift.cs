using TopSpeed.Common;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Events;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void HandleTransmissionInput(in CarControlIntent intent)
        {
            if (_manualTransmission)
            {
                HandleManualShift(intent.GearUp, intent.GearDown, intent.Clutch);
                return;
            }

            HandleAutomaticDirectionShift(intent.ReverseRequested, intent.ForwardRequested);
        }

        private void HandleManualShift(bool gearUp, bool gearDown, int clutch)
        {
            if (!gearUp && !gearDown)
                _stickReleased = true;

            if (gearDown && _stickReleased)
            {
                if (!CanShiftManual(clutch))
                {
                    _stickReleased = false;
                    _soundBadSwitch.Play(loop: false);
                    return;
                }

                if (_gear > FirstForwardGear)
                {
                    _stickReleased = false;
                    _switchingGear = -1;
                    --_gear;
                    if (_soundEngine.GetPitch() > 3f * _topFreq / (2f * _soundEngine.InputSampleRate))
                        _soundBadSwitch.Play(loop: false);
                    if (!AnyBackfirePlaying() && Algorithm.RandomInt(5) == 1)
                        PlayRandomBackfire();
                    PushEvent(EventType.InGear, 0.2f);
                }
                else if (_gear == FirstForwardGear)
                {
                    _stickReleased = false;
                    if (_speed <= ReverseShiftMaxSpeedKmh)
                    {
                        _switchingGear = -1;
                        _gear = ReverseGear;
                        PushEvent(EventType.InGear, 0.2f);
                    }
                    else
                    {
                        _soundBadSwitch.Play(loop: false);
                    }
                }
            }
            else if (gearUp && _stickReleased)
            {
                if (!CanShiftManual(clutch))
                {
                    _stickReleased = false;
                    _soundBadSwitch.Play(loop: false);
                    return;
                }

                if (_gear == ReverseGear)
                {
                    _stickReleased = false;
                    if (_speed <= ReverseShiftMaxSpeedKmh)
                    {
                        _switchingGear = 1;
                        _gear = FirstForwardGear;
                        PushEvent(EventType.InGear, 0.2f);
                    }
                    else
                    {
                        _soundBadSwitch.Play(loop: false);
                    }
                }
                else if (_gear < _gears)
                {
                    _stickReleased = false;
                    _switchingGear = 1;
                    ++_gear;
                    if (_soundEngine.GetPitch() < _idleFreq / (float)_soundEngine.InputSampleRate)
                        _soundBadSwitch.Play(loop: false);
                    if (!AnyBackfirePlaying() && Algorithm.RandomInt(5) == 1)
                        PlayRandomBackfire();
                    PushEvent(EventType.InGear, 0.2f);
                }
            }
        }

        private static bool CanShiftManual(int clutch)
        {
            return clutch >= 90;
        }

        private void HandleAutomaticDirectionShift(bool reverseRequested, bool forwardRequested)
        {
            if (reverseRequested && _gear != ReverseGear)
            {
                if (_speed <= ReverseShiftMaxSpeedKmh)
                {
                    _switchingGear = -1;
                    _gear = ReverseGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
                else
                {
                    _currentThrottle = 0;
                    _currentBrake = -100;
                    _soundBadSwitch.Play(loop: false);
                }
            }
            else if (forwardRequested && _gear == ReverseGear)
            {
                if (_speed <= ReverseShiftMaxSpeedKmh)
                {
                    _switchingGear = 1;
                    _gear = FirstForwardGear;
                    PushEvent(EventType.InGear, 0.2f);
                }
                else
                {
                    _soundBadSwitch.Play(loop: false);
                }
            }
        }
    }
}
