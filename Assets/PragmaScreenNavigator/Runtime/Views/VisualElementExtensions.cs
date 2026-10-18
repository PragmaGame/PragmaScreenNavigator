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
    }
}
