using UnityEngine;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// Keeps the content of every child of a container inside the screen's safe area (<see cref="Screen.safeArea"/>: a
    /// camera cutout, rounded corners, the system bars), while the child itself stays on the whole container: its
    /// background — a dimming, a curtain — and its pointer catching cover the cutout too, so nothing behind a modal screen
    /// takes a touch there. The margins of the safe area, in the units of the panel, are a transparent border of each child:
    /// the layout keeps both the child's flow children and its absolutely positioned ones inside the border, which padding
    /// would not do for the absolute ones, and the background is drawn under it. The hosts use it with the navigation root
    /// as the container, so every screen gets it; the border of a screen's root is theirs, frame a screen on an inner
    /// element. <see cref="Update"/> is cheap while nothing changes, so call it every frame: the safe area changes with the
    /// orientation and the resolution.
    /// </summary>
    public sealed class SafeAreaInset
    {
        private readonly VisualElement _container;

        /// <param name="container">The container whose children get the inset: the navigation root, for its screens.</param>
        public SafeAreaInset(VisualElement container)
        {
            _container = container;
        }

        /// <summary>Applies the safe area of the screen to every child whose border does not match it yet.</summary>
        public void Update()
        {
            Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        /// <summary>
        /// Applies <paramref name="safeArea"/> of a screen of <paramref name="screenSize"/> pixels, in the units of the
        /// container's panel, to every child whose border does not match it yet. Nothing happens while the container is not in
        /// a laid out panel.
        /// </summary>
        /// <param name="safeArea">Pixels from the bottom left of the screen, as <see cref="Screen.safeArea"/>.</param>
        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            var root = _container.panel?.visualTree;

            if (root == null)
            {
                return;
            }

            SetInsets(GetInsets(safeArea, screenSize, root.layout.size));
        }

        /// <summary>
        /// Gives every child of the container a transparent border of <paramref name="insets"/>, in panel units. Each child is
        /// checked on every call and written only when its border differs: a screen that came in since (a replaced tab, one
        /// back from its pool after the screen turned) gets the current inset whatever the number of children.
        /// </summary>
        public void SetInsets((float left, float top, float right, float bottom) insets)
        {
            for (var i = 0; i < _container.childCount; i++)
            {
                var child = _container[i];

                if (Matches(child.style.borderLeftWidth, insets.left) && Matches(child.style.borderTopWidth, insets.top)
                    && Matches(child.style.borderRightWidth, insets.right) && Matches(child.style.borderBottomWidth, insets.bottom))
                {
                    continue;
                }

                child.style.borderLeftWidth = insets.left;
                child.style.borderTopWidth = insets.top;
                child.style.borderRightWidth = insets.right;
                child.style.borderBottomWidth = insets.bottom;
                child.style.borderLeftColor = Color.clear;
                child.style.borderTopColor = Color.clear;
                child.style.borderRightColor = Color.clear;
                child.style.borderBottomColor = Color.clear;
            }
        }

        /// <summary>Puts the content of the children back on their whole area.</summary>
        public void Reset()
        {
            SetInsets((0f, 0f, 0f, 0f));
        }

        /// <summary>The border width set on the child is <paramref name="inset"/>; one never set is no border.</summary>
        private static bool Matches(StyleFloat width, float inset)
        {
            return width.keyword == StyleKeyword.Undefined ? Mathf.Approximately(width.value, inset) : inset == 0f;
        }

        /// <summary>
        /// The margins of <paramref name="safeArea"/> on a screen of <paramref name="screenSize"/> pixels, in the units of a
        /// panel of <paramref name="panelSize"/> over the whole screen; zero when a size is not known yet.
        /// </summary>
        /// <param name="safeArea">Pixels from the bottom left of the screen, as <see cref="Screen.safeArea"/>.</param>
        public static (float left, float top, float right, float bottom) GetInsets(Rect safeArea, Vector2 screenSize, Vector2 panelSize)
        {
            // A panel that has not been laid out yet has a NaN size: the comparisons below are false for it.
            if (!(screenSize.x > 0f && screenSize.y > 0f && panelSize.x > 0f && panelSize.y > 0f))
            {
                return (0f, 0f, 0f, 0f);
            }

            var scaleX = panelSize.x / screenSize.x;
            var scaleY = panelSize.y / screenSize.y;

            // The screen counts from the bottom left, the panel from the top left.
            return (Mathf.Max(0f, safeArea.xMin) * scaleX,
                    Mathf.Max(0f, screenSize.y - safeArea.yMax) * scaleY,
                    Mathf.Max(0f, screenSize.x - safeArea.xMax) * scaleX,
                    Mathf.Max(0f, safeArea.yMin) * scaleY);
        }
    }
}
