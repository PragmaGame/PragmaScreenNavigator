using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// A list of rows with a selection, an element of a screen over a <see cref="SelectableListViewModel"/>: one row per
    /// <see cref="ListRowViewModel"/>, the selected one with the <c>--selected</c> modifier of the row class. The UXML sets the
    /// row class (<see cref="RowClass"/>: how rows look is the screen's style) and whether rows can be selected
    /// (<see cref="IsSelectable"/>: rows are buttons that select on click; otherwise labels whose mark only shows). The data is
    /// the list view model the screen's view model owns; the screen view binds it in OnBind (<see cref="Bind"/>) and
    /// unbinds it in OnUnbind.
    /// </summary>
    [UxmlElement]
    public partial class SelectableList : VisualElement
    {
        /// <summary>The default <see cref="RowClass"/>.</summary>
        public const string ROW_USS_CLASS_NAME = "selectable-list__row";

        /// <summary>The modifier the selected row gets on its row class.</summary>
        public const string SELECTED_USS_MODIFIER = "--selected";

        private readonly List<TextElement> _rows = new List<TextElement>();
        private readonly List<ListRowViewModel> _bound = new List<ListRowViewModel>();

        private SelectableListViewModel _viewModel;

        /// <summary>The USS class of the rows; the selected one also gets it with <see cref="SELECTED_USS_MODIFIER"/>.</summary>
        [UxmlAttribute]
        public string RowClass { get; set; } = ROW_USS_CLASS_NAME;

        /// <summary>Rows are buttons, and a click selects one; otherwise they are labels.</summary>
        [UxmlAttribute]
        public bool IsSelectable { get; set; } = true;

        /// <summary>Shows <paramref name="viewModel"/>, after ending the binding of the previous one.</summary>
        public void Bind(SelectableListViewModel viewModel)
        {
            Unbind();

            _viewModel = viewModel;
            dataSource = viewModel;
            _viewModel.propertyChanged += OnListChanged;
            Render();
        }

        /// <summary>Stops following the view model; the rows keep what they show.</summary>
        public void Unbind()
        {
            if (_viewModel == null)
            {
                return;
            }

            _viewModel.propertyChanged -= OnListChanged;
            UnbindRows();
            _viewModel = null;
            dataSource = null;
        }

        private void OnListChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            if (args.propertyName == nameof(SelectableListViewModel.Rows))
            {
                Render();
            }
        }

        private void Render()
        {
            UnbindRows();

            var rows = _viewModel.Rows;

            while (_rows.Count < rows.Count)
            {
                _rows.Add(CreateRow(_rows.Count));
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var isShown = i < rows.Count;
                _rows[i].SetDisplayed(isShown);

                if (!isShown)
                {
                    continue;
                }

                var row = rows[i];
                row.propertyChanged += OnRowChanged;
                _bound.Add(row);
                _rows[i].dataSource = row;
                Show(i);
            }
        }

        private TextElement CreateRow(int index)
        {
            TextElement row = IsSelectable ? new Button(() => _viewModel?.Select(index)) : new Label();

            row.AddToClassList(RowClass);
            Add(row);

            return row;
        }

        private void OnRowChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            var index = _bound.IndexOf((ListRowViewModel)sender);

            if (index >= 0)
            {
                Show(index);
            }
        }

        private void Show(int index)
        {
            var row = _bound[index];

            _rows[index].text = row.Text;
            _rows[index].EnableInClassList(RowClass + SELECTED_USS_MODIFIER, row.IsSelected);
        }

        private void UnbindRows()
        {
            for (var i = 0; i < _bound.Count; i++)
            {
                _bound[i].propertyChanged -= OnRowChanged;
                _rows[i].dataSource = null;
            }

            _bound.Clear();
        }
    }
}
