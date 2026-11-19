using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary><see cref="SafeAreaInset"/>: the margins of the safe area in panel units and what it does to an element.</summary>
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
        public void Apply_OutsideAPanel_LeavesTheElement_AndResetPutsItOnTheWholePanel()
        {
            var element = new VisualElement();
            element.style.left = 7f;
            var inset = new SafeAreaInset(element);

            inset.Apply(new Rect(120f, 0f, SCREEN.x - 120f, SCREEN.y), SCREEN);
            Assert.AreEqual(7f, element.style.left.value.value, "no panel, no size to inset by");

            inset.Reset();
            Assert.AreEqual(0f, element.style.left.value.value);
            Assert.AreEqual(0f, element.style.top.value.value);
            Assert.AreEqual(0f, element.style.right.value.value);
            Assert.AreEqual(0f, element.style.bottom.value.value);
        }
    }
}
