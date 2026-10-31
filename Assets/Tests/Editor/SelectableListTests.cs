using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary><see cref="SelectableListViewModel"/> and the <see cref="SelectableList"/> element that shows it.</summary>
    public class SelectableListTests
    {
        [Test]
        public void ViewModel_SetRows_ReusesRowsAndNotifiesTheCountAndTheSelection()
        {
            var list = new SelectableListViewModel();
            var notified = new List<string>();
            list.propertyChanged += (_, args) => notified.Add(args.propertyName.ToString());

            list.SetRows(new[] { "a", "b" }, 1);
            CollectionAssert.AreEqual(new[] { nameof(SelectableListViewModel.Rows), nameof(SelectableListViewModel.Selected) }, notified);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list.Selected);
            Assert.IsFalse(list.Rows[0].IsSelected);
            Assert.IsTrue(list.Rows[1].IsSelected);

            notified.Clear();
            var first = list.Rows[0];
            var second = list.Rows[1];
            list.SetRows(new[] { "c", "d" }, 1);
            CollectionAssert.IsEmpty(notified, "the same count and selection: the rows notify their texts themselves");
            Assert.AreSame(first, list.Rows[0]);
            Assert.AreEqual("c", list.Rows[0].Text);

            list.SetRows(new[] { "e" }, -1);
            Assert.IsFalse(second.IsSelected, "a removed row is unmarked");
            list.SetRows(new[] { "e", "f" }, 5);
            Assert.AreSame(second, list.Rows[1], "a removed row serves again");
            Assert.AreEqual(-1, list.Selected, "an index outside the list selects nothing");
        }

        [Test]
        public void ViewModel_Select_ReportsThePlayersChoiceOnly()
        {
            var list = new SelectableListViewModel();
            var chosen = new List<int>();
            list.Chosen += chosen.Add;

            list.SetRows(new[] { "a", "b", "c" }, 0);
            CollectionAssert.IsEmpty(chosen, "the screen's own selection is not reported");

            list.Select(2);
            list.Select(7);
            CollectionAssert.AreEqual(new[] { 2, -1 }, chosen);
            Assert.AreEqual(-1, list.Selected);
        }

        [Test]
        public void Element_ShowsTheRowsAndFollowsTheirChanges()
        {
            var list = new SelectableListViewModel();
            list.SetRows(new[] { "a", "b", "c" }, 1);
            var element = new SelectableList { RowClass = "row" };

            element.Bind(list);
            var rows = element.Children().OfType<Button>().ToArray();
            Assert.AreEqual(3, rows.Length, "selectable rows are buttons");
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows.Select(row => row.text));
            Assert.IsTrue(rows[1].ClassListContains("row--selected"));
            Assert.IsFalse(rows[0].ClassListContains("row--selected"));

            list.Select(0);
            Assert.IsTrue(rows[0].ClassListContains("row--selected"));
            Assert.IsFalse(rows[1].ClassListContains("row--selected"));

            list.SetRows(new[] { "x" }, -1);
            Assert.AreEqual("x", rows[0].text);
            Assert.AreEqual(DisplayStyle.None, rows[1].style.display.value, "rows beyond the list are hidden, not removed");
            Assert.AreEqual(3, element.childCount);

            element.Unbind();
            list.SetRows(new[] { "y" }, 0);
            Assert.AreEqual("x", rows[0].text, "an unbound element keeps what it showed");
        }

        [Test]
        public void Element_NotSelectable_ShowsLabels()
        {
            var list = new SelectableListViewModel();
            list.SetRows(new[] { "me", "you" }, 0);
            var element = new SelectableList { IsSelectable = false };

            element.Bind(list);
            var rows = element.Children().ToArray();
            Assert.IsTrue(rows.All(row => row is Label));
            Assert.IsTrue(rows[0].ClassListContains(SelectableList.ROW_USS_CLASS_NAME + SelectableList.SELECTED_USS_MODIFIER));
        }
    }
}
