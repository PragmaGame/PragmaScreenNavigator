using UnityEngine;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// Keeps an absolutely positioned element inside the screen's safe area (<see cref="Screen.safeArea"/>: a camera
    /// cutout, rounded corners, the system bars): its left, top, right and bottom offsets are the margins of the safe area,
    /// in the units of its panel. What lies under the element's parent outside the safe area — the game world under a
    /// cutout — stays on the whole screen; only the element and its children move in. <see cref="Update"/> is cheap while
    /// nothing changes, so call it every frame: the safe area changes with the orientation and the resolution.
    /// </summary>
    public sealed class SafeAreaInset
    {
        private readonly VisualElement _element;

        private Rect _safeArea;
        private Vector2 _screenSize;
        private Vector2 _panelSize;

        /// <param name="element">An element with <see cref="Position.Absolute"/>: its offsets are what this class sets.</param>
        public SafeAreaInset(VisualElement element)
        {
            _element = element;
        }

        /// <summary>Applies the safe area of the screen, when it, the screen or the panel size changed since the last call.</summary>
        public void Update()
        {
            Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        /// <summary>
        /// Applies <paramref name="safeArea"/> of a screen of <paramref name="screenSize"/> pixels, when one of them or the size
        /// of the element's panel changed since the last call. Nothing happens while the element is not in a laid out panel.
        /// </summary>
        /// <param name="safeArea">Pixels from the bottom left of the screen, as <see cref="Screen.safeArea"/>.</param>
        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            var root = _element.panel?.visualTree;

            if (root == null)
            {
                return;
            }

            var panelSize = root.layout.size;

            if (safeArea == _safeArea && screenSize == _screenSize && panelSize == _panelSize)
            {
                return;
            }

            _safeArea = safeArea;
            _screenSize = screenSize;
            _panelSize = panelSize;

            var (left, top, right, bottom) = GetInsets(safeArea, screenSize, panelSize);
            _element.style.left = left;
            _element.style.top = top;
            _element.style.right = right;
            _element.style.bottom = bottom;
        }

        /// <summary>Puts the element back on the whole panel, and the next <see cref="Apply"/> sets the offsets anew.</summary>
        public void Reset()
        {
            _safeArea = default;
            _screenSize = default;
            _panelSize = default;
            _element.style.left = 0;
            _element.style.top = 0;
            _element.style.right = 0;
            _element.style.bottom = 0;
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
