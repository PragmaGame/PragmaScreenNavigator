using System;
using System.Collections.Generic;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The items of a <see cref="SelectableList"/>: what each row stands for, its text and the selected item. The selection
    /// follows an item by its key, so it survives a refill of the list with new data (from disk or a service) while that
    /// item is still there. The rows and the player's choice are the <see cref="List"/> view model the screen binds; the
    /// screen's view model owns this object and reads the selected item here.
    /// </summary>
    public sealed class SelectableItems<TItem>
    {
        private readonly Func<TItem, string> _text;
        private readonly Func<TItem, object> _key;
        private readonly List<string> _texts = new List<string>();

        private IReadOnlyList<TItem> _items = Array.Empty<TItem>();

        /// <param name="text">The text of an item's row.</param>
        /// <param name="key">What identifies an item between refills (an id, a file name); compared with <see cref="object.Equals(object, object)"/>.</param>
        public SelectableItems(Func<TItem, string> text, Func<TItem, object> key)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>The rows and the selection, for the <see cref="SelectableList"/> of the screen.</summary>
        public SelectableListViewModel List { get; } = new SelectableListViewModel();

        public IReadOnlyList<TItem> Items => _items;

        /// <summary>The index of the selected item; -1 when none is.</summary>
        public int Selected => List.Selected;

        public bool HasSelection => List.Selected >= 0;

        /// <summary>The selected item; the default value when none is.</summary>
        public TItem SelectedItem => HasSelection ? _items[List.Selected] : default;

        /// <summary>
        /// Replaces the items: the selection stays on the item with the key of the selected one; when it is gone, the first
        /// item is selected if <paramref name="isFirstSelected"/>, otherwise none.
        /// </summary>
        public void SetItems(IReadOnlyList<TItem> items, bool isFirstSelected)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var selectedKey = HasSelection ? _key(_items[List.Selected]) : null;
            var selected = -1;

            _items = items;
            _texts.Clear();

            for (var i = 0; i < items.Count; i++)
            {
                _texts.Add(_text(items[i]));

                if (selected < 0 && selectedKey != null && Equals(_key(items[i]), selectedKey))
                {
                    selected = i;
                }
            }

            if (selected < 0 && isFirstSelected && items.Count > 0)
            {
                selected = 0;
            }

            List.SetRows(_texts, selected);
        }

        /// <summary>Selects the item at <paramref name="index"/>, or none for an index outside the list, as the screen's own choice.</summary>
        public void Select(int index)
        {
            List.SetRows(_texts, index);
        }
    }
}
