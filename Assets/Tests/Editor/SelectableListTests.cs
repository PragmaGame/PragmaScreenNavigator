using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>
    /// <see cref="SelectableListViewModel"/>, the <see cref="SelectableList"/> element that shows it and the items behind its
    /// rows (<see cref="SelectableItems{TItem}"/>).
    /// </summary>
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
        public void Items_SelectionFollowsTheItemsKey_ThroughARefill()
        {
            var items = new SelectableItems<(string id, string name)>(item => item.name, item => item.id);
            items.SetItems(new[] { ("a", "Alpha"), ("b", "Beta") }, false);
            Assert.IsFalse(items.HasSelection, "nothing selected unless asked");
            CollectionAssert.AreEqual(new[] { "Alpha", "Beta" }, items.List.Rows.Select(row => row.Text));

            items.List.Select(1);
            Assert.AreEqual(("b", "Beta"), items.SelectedItem, "the player's choice is the selected item");

            items.SetItems(new[] { ("c", "Gamma"), ("b", "Beta renamed"), ("a", "Alpha") }, false);
            Assert.AreEqual(1, items.Selected, "the selection moved with its item");
            Assert.AreEqual("Beta renamed", items.SelectedItem.name);

            items.SetItems(new[] { ("c", "Gamma") }, false);
            Assert.IsFalse(items.HasSelection, "the selected item is gone");
            Assert.AreEqual(default((string, string)), items.SelectedItem);

            items.SetItems(new[] { ("c", "Gamma"), ("d", "Delta") }, true);
            Assert.AreEqual(0, items.Selected, "nothing to follow: the first item when asked");
        }

        [Test]
        public void Items_Select_IsTheScreensOwnChoice_NotReported()
        {
            var items = new SelectableItems<string>(item => item, item => item);
            var chosen = new List<int>();
            items.List.Chosen += chosen.Add;
            items.SetItems(new[] { "a", "b" }, true);

            items.Select(1);

            Assert.AreEqual("b", items.SelectedItem);
            CollectionAssert.IsEmpty(chosen);
        }

        [Test]
        public void DisabledRow_IsGreyed_AndThePlayerCannotChooseIt()
        {
            var list = new SelectableListViewModel();
            var chosen = new List<int>();
            list.Chosen += chosen.Add;
            list.SetRows(new[] { "a", "b" }, 0);
            list.SetEnabled(1, false);
            var element = new SelectableList();
            element.Bind(list);
            var rows = element.Children().OfType<Button>().ToArray();

            Assert.IsFalse(rows[1].enabledSelf, "the row is greyed out");
            list.Select(1);
            Assert.AreEqual(0, list.Selected, "a disabled row cannot be chosen");
            CollectionAssert.IsEmpty(chosen, "nothing to report");

            list.SetSelected(1);
            Assert.AreEqual(1, list.Selected, "the screen may select it itself");
            CollectionAssert.IsEmpty(chosen);

            list.SetRows(new[] { "c", "d" }, -1);
            Assert.IsTrue(list.Rows[1].IsEnabled, "new rows are enabled again");
            Assert.IsTrue(rows[1].enabledSelf);
        }

        [Test]
        public void Items_DisabledItems_AreSkippedForTheFirstSelection()
        {
            var items = new SelectableItems<(string id, bool isReady)>(item => item.id, item => item.id, item => item.isReady);

            items.SetItems(new[] { ("a", false), ("b", true) }, true);

            Assert.AreEqual(1, items.Selected, "the first item the player can pick");
            Assert.IsFalse(items.List.Rows[0].IsEnabled);
            Assert.IsTrue(items.List.Rows[1].IsEnabled);

            items.List.Select(0);
            Assert.AreEqual(1, items.Selected, "the player cannot pick the disabled one");
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
