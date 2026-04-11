using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles.Events
{
    internal sealed class EventQueue
    {
        private readonly List<EventEntry> _items = new List<EventEntry>();

        public void Push(float dueTime, EventType type, VibrationEffectType? effect = null)
        {
            _items.Add(new EventEntry
            {
                Type = type,
                Time = dueTime,
                Effect = effect
            });
        }

        public void DrainDue(float now, Action<EventEntry> onDue)
        {
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item.Time >= now)
                    continue;
                onDue(item);
                _items.RemoveAt(i);
            }
        }

        public void RemoveAll(params EventType[] types)
        {
            if (types == null || types.Length == 0)
                return;

            for (var i = _items.Count - 1; i >= 0; i--)
            {
                var itemType = _items[i].Type;
                for (var typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    if (itemType != types[typeIndex])
                        continue;

                    _items.RemoveAt(i);
                    break;
                }
            }
        }
    }
}

