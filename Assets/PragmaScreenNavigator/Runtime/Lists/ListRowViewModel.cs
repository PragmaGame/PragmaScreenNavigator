using Unity.Properties;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// A row of a <see cref="SelectableList"/>: its text and whether it is selected. A part of a screen with data of its own
    /// but no lifecycle, so a <see cref="ViewModel"/> directly: the list view model (<see cref="SelectableListViewModel"/>)
    /// owns it, and the row element makes it its dataSource.
    /// </summary>
    [GeneratePropertyBag]
    public class ListRowViewModel : ViewModel
    {
        private string _text = string.Empty;
        private bool _isSelected;

        [CreateProperty]
        public string Text
        {
            get => _text;
            internal set => SetProperty(ref _text, value ?? string.Empty);
        }

        /// <summary>The row is selected; in a list the player cannot select from, it is marked (the player's own row in a lobby).</summary>
        [CreateProperty]
        public bool IsSelected
        {
            get => _isSelected;
            internal set => SetProperty(ref _isSelected, value);
        }
    }
}
