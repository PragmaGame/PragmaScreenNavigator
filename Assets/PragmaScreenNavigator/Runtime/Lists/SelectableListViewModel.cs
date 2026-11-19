using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// A list of rows with a selection, the view model of a <see cref="SelectableList"/>: the rows
    /// (<see cref="ListRowViewModel"/>), the selected one and the player's choice. The view model of the screen owns it (a
    /// list is a part of a screen without a lifecycle of its own) and fills it with <see cref="SetRows"/>; the screen learns
    /// what the player picked from <see cref="Chosen"/>. The notifications are separate: <see cref="Rows"/> means the number of
    /// rows changed, <see cref="Selected"/> the selection; a row notifies its own text, mark and whether it is enabled. Rows
    /// are reused, so a list that refreshes itself does not create view models over and over. A row the player must not pick
    /// (an item that is not ready) is disabled with <see cref="SetEnabled"/>: it stays in the list, greyed out.
    /// </summary>
    [GeneratePropertyBag]
    public class SelectableListViewModel : ViewModel
    {
        private readonly List<ListRowViewModel> _rows = new List<ListRowViewModel>();
        private readonly Stack<ListRowViewModel> _spare = new Stack<ListRowViewModel>();

        private int _selected = -1;

        /// <summary>The player selected a row (<see cref="Select"/>): its index, -1 for none. A selection made by <see cref="SetRows"/> is not reported: the screen made it.</summary>
        public event Action<int> Chosen;

        /// <summary>The rows in order; a notification of this property means their number changed.</summary>
        public IReadOnlyList<ListRowViewModel> Rows => _rows;

        /// <summary>The index of the selected row; -1 when none is.</summary>
        [CreateProperty]
        public int Selected => _selected;

        public int Count => _rows.Count;

        /// <summary>Replaces the rows: their texts in order and the selected one (-1 for none). Every row is enabled again.</summary>
        public void SetRows(IReadOnlyList<string> texts, int selected)
        {
            var isCountChanged = texts.Count != _rows.Count;

            while (_rows.Count > texts.Count)
            {
                var row = _rows[_rows.Count - 1];
                _rows.RemoveAt(_rows.Count - 1);
                row.IsSelected = false;
                _spare.Push(row);
            }

            while (_rows.Count < texts.Count)
            {
                _rows.Add(_spare.Count > 0 ? _spare.Pop() : new ListRowViewModel());
            }

            for (var i = 0; i < texts.Count; i++)
            {
                _rows[i].Text = texts[i];
                _rows[i].IsEnabled = true;
            }

            if (isCountChanged)
            {
                NotifyPropertyChanged(nameof(Rows));
            }

            ApplySelection(selected);
        }

        /// <summary>
        /// The player's choice: the row at <paramref name="index"/>, or none for an index outside the list. A disabled row
        /// cannot be chosen: nothing changes and nothing is reported.
        /// </summary>
        public void Select(int index)
        {
            if (index >= 0 && index < _rows.Count && !_rows[index].IsEnabled)
            {
                return;
            }

            ApplySelection(index);
            Chosen?.Invoke(_selected);
        }

        /// <summary>The screen's own selection: the row at <paramref name="index"/>, or none for an index outside the list; not reported.</summary>
        public void SetSelected(int index)
        {
            ApplySelection(index);
        }

        /// <summary>Whether the player can select the row at <paramref name="index"/>; the screen's own selection is not limited by it.</summary>
        public void SetEnabled(int index, bool isEnabled)
        {
            _rows[index].IsEnabled = isEnabled;
        }

        private void ApplySelection(int index)
        {
            var selected = index >= 0 && index < _rows.Count ? index : -1;

            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].IsSelected = i == selected;
            }

            if (selected != _selected)
            {
                _selected = selected;
                NotifyPropertyChanged(nameof(Selected));
            }
        }
    }
}
