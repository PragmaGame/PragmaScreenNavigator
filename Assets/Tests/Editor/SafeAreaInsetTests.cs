using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>
    /// <see cref="SafeAreaInset"/>: the margins of the safe area in panel units, and the transparent border of the children
    /// that keeps their content inside it while they stay on the whole container.
    /// </summary>
    public class SafeAreaInsetTests
    {
        private static readonly Vector2 SCREEN = new Vector2(2400f, 1080f);

        [Test]
        public void GetInsets_TheWholeScreen_IsNoInset()
        {
            Assert.AreEqual((0f, 0f, 0f, 0f), SafeAreaInset.GetInsets(new Rect(Vector2.zero, SCREEN), SCREEN, SCREEN));
        }

        [Test]
        public void GetInsets_ACutoutOnTheLeft_InsetsTheLeft_InPanelUnits()
        {
            // A punch-hole camera on the left of a landscape phone, a panel at half the pixels.
            var safeArea = new Rect(120f, 0f, SCREEN.x - 120f, SCREEN.y);

            Assert.AreEqual((60f, 0f, 0f, 0f), SafeAreaInset.GetInsets(safeArea, SCREEN, SCREEN / 2f));
        }

        [Test]
        public void GetInsets_TheScreenCountsFromTheBottom_ThePanelFromTheTop()
        {
            // A status bar at the top (40 px) and a gesture bar at the bottom (20 px) of a portrait screen.
            var screen = new Vector2(1080f, 2400f);
            var safeArea = new Rect(0f, 20f, screen.x, screen.y - 60f);

            Assert.AreEqual((0f, 40f, 0f, 20f), SafeAreaInset.GetInsets(safeArea, screen, screen));
        }

        [Test]
        public void GetInsets_UnknownSizes_AreNoInset()
        {
            var safeArea = new Rect(120f, 0f, SCREEN.x - 120f, SCREEN.y);

            Assert.AreEqual((0f, 0f, 0f, 0f), SafeAreaInset.GetInsets(safeArea, Vector2.zero, SCREEN), "no screen size");
            Assert.AreEqual((0f, 0f, 0f, 0f), SafeAreaInset.GetInsets(safeArea, SCREEN, new Vector2(float.NaN, float.NaN)),
                            "a panel that has not been laid out");
        }

        [Test]
        public void Apply_OutsideAPanel_LeavesTheChildren()
        {
            var container = new VisualElement();
            var child = new VisualElement();
            container.Add(child);
            var inset = new SafeAreaInset(container);

            inset.Apply(new Rect(120f, 0f, SCREEN.x - 120f, SCREEN.y), SCREEN);

            Assert.AreEqual(StyleKeyword.Null, child.style.borderLeftWidth.keyword, "no panel, no size to inset by");
        }

        [Test]
        public void SetInsets_IsATransparentBorderOfEveryChild_AndOfAChildAddedSince()
        {
            var container = new VisualElement();
            var first = new VisualElement();
            container.Add(first);
            container.Add(new VisualElement());
            var inset = new SafeAreaInset(container);
            var insets = (10f, 20f, 30f, 40f);

            inset.SetInsets(insets);

            foreach (var child in container.Children())
            {
                Assert.AreEqual(insets, (child.style.borderLeftWidth.value, child.style.borderTopWidth.value, child.style.borderRightWidth.value,
                                         child.style.borderBottomWidth.value), "the content moves in by the margins");
                Assert.AreEqual(Color.clear, child.style.borderLeftColor.value, "the border shows the background, not a frame");
            }

            Assert.AreEqual(StyleKeyword.Null, first.style.left.keyword, "the child itself stays where its own style puts it");

            var added = new VisualElement();
            container.Add(added);
            inset.SetInsets(insets);
            Assert.AreEqual(30f, added.style.borderRightWidth.value, "a screen created since gets the same inset");

            inset.Reset();
            Assert.AreEqual(0f, first.style.borderLeftWidth.value);
            Assert.AreEqual(0f, added.style.borderBottomWidth.value);
        }

        [Test]
        public void SetInsets_AChildReplacedBetweenCalls_GetsTheBorder()
        {
            var container = new VisualElement();
            var leaving = new VisualElement();
            container.Add(leaving);
            container.Add(new VisualElement());
            var inset = new SafeAreaInset(container);
            var insets = (10f, 20f, 30f, 40f);
            inset.SetInsets(insets);

            // A tab replaced by another: the same number of children, a new one among them.
            leaving.RemoveFromHierarchy();
            var coming = new VisualElement();
            container.Add(coming);
            inset.SetInsets(insets);

            Assert.AreEqual(10f, coming.style.borderLeftWidth.value);
            Assert.AreEqual(40f, coming.style.borderBottomWidth.value);
        }

        [Test]
        public void SetInsets_AChildBackAfterTheInsetsChanged_GetsTheNewOnes()
        {
            var container = new VisualElement();
            var pooled = new VisualElement();
            container.Add(pooled);
            container.Add(new VisualElement());
            var inset = new SafeAreaInset(container);
            inset.SetInsets((10f, 0f, 0f, 0f));

            // A screen asleep in its pool outside the root while the phone turns, then back.
            pooled.RemoveFromHierarchy();
            inset.SetInsets((0f, 0f, 10f, 0f));
            container.Add(pooled);
            inset.SetInsets((0f, 0f, 10f, 0f));

            Assert.AreEqual(0f, pooled.style.borderLeftWidth.value, "the old inset is gone");
            Assert.AreEqual(10f, pooled.style.borderRightWidth.value, "the new one is there");
        }
    }
}
