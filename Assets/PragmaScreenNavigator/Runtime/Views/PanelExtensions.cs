using UnityEngine;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// What the UI of a runtime panel (a <see cref="UIDocument"/>) takes from the game's input: the pointer over its
    /// elements and the keyboard while a text field has the focus. Ask them before treating a click or a key as one for the
    /// world under the UI: selecting units, an order, a camera hotkey.
    /// </summary>
    public static class PanelExtensions
    {
        /// <summary>
        /// The pointer at <paramref name="screenPosition"/> is over an element of the panel that catches it, so a click there
        /// belongs to the UI. Elements with <see cref="PickingMode.Ignore"/> let the pointer through to the world: the
        /// navigation root and screens have it, give it to the root of the document as well. False for null.
        /// </summary>
        /// <param name="screenPosition">Pixels from the bottom left of the screen, as the pointer positions of the Input System and <c>Input.mousePosition</c>.</param>
        public static bool IsPointerOverUi(this IPanel panel, Vector2 screenPosition)
        {
            if (panel == null)
            {
                return false;
            }

            // The panel counts from the top left.
            var position = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            var picked = panel.Pick(position);

            return picked != null && picked != panel.visualTree;
        }

        /// <summary>
        /// The keyboard focus of the panel is in a text input field (<see cref="VisualElementExtensions.IsTextInput"/>): the
        /// player types, and the game's hotkeys should stay silent. False for null.
        /// </summary>
        public static bool IsTyping(this IPanel panel)
        {
            return panel?.focusController?.focusedElement is VisualElement focused && focused.IsTextInput();
        }
    }
}
