using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    public static class VisualElementExtensions
    {
        /// <summary>Shows the element or takes it out of the layout (display): this is how views hide empty panels and rows.</summary>
        public static void SetDisplayed(this VisualElement element, bool isDisplayed)
        {
            element.style.display = isDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// The element is a text input field or a part of one (<see cref="TextField"/>, <see cref="IntegerField"/> and the
        /// other fields the player types into): what holds the focus while the player types. False for null.
        /// </summary>
        public static bool IsTextInput(this VisualElement element)
        {
            // Every field built on TextInputBaseField carries its class, whatever its value type.
            for (var current = element; current != null; current = current.parent)
            {
                if (current.ClassListContains(TextInputBaseField<string>.ussClassName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
