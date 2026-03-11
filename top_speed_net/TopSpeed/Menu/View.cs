using System;
using System.Collections.Generic;
using TopSpeed.Speech;

namespace TopSpeed.Menu
{
    internal sealed class MenuView
    {
        private readonly List<MenuItem> _items = new List<MenuItem>();
        private readonly List<MenuShortcut> _shortcuts = new List<MenuShortcut>();
        private int _savedSelection = -1;

        public MenuView(
            string id,
            IEnumerable<MenuItem> items,
            string? title = null,
            Func<string>? titleProvider = null,
            bool preserveSelection = false,
            SpeechService.SpeakFlag titleSpeakFlag = SpeechService.SpeakFlag.Interruptable,
            IEnumerable<MenuShortcut>? shortcuts = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Screen id is required.", nameof(id));

            Id = id.Trim();
            Title = title ?? string.Empty;
            TitleProvider = titleProvider;
            PreserveSelection = preserveSelection;
            TitleSpeakFlag = titleSpeakFlag;
            ReplaceItems(items);
            SetShortcuts(shortcuts);
        }

        public string Id { get; }
        public string Title { get; set; }
        public Func<string>? TitleProvider { get; set; }
        public bool PreserveSelection { get; set; }
        public SpeechService.SpeakFlag TitleSpeakFlag { get; set; }
        public IReadOnlyList<MenuItem> Items => _items;
        public IReadOnlyList<MenuShortcut> Shortcuts => _shortcuts;
        public string DisplayTitle => TitleProvider?.Invoke() ?? Title;

        internal int SavedSelection
        {
            get => _savedSelection;
            set => _savedSelection = value;
        }

        public void ReplaceItems(IEnumerable<MenuItem> items)
        {
            _items.Clear();
            if (items == null)
                return;

            foreach (var item in items)
            {
                if (item == null || item.IsHidden)
                    continue;
                _items.Add(item);
            }
        }

        public void SetShortcuts(IEnumerable<MenuShortcut>? shortcuts)
        {
            _shortcuts.Clear();
            if (shortcuts == null)
                return;

            _shortcuts.AddRange(shortcuts);
        }
    }
}
