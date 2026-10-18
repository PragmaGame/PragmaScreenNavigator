using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// Keeps input away from a screen out of focus: pointer, keyboard and UI Toolkit navigation events stop at the screen
    /// root before they reach its buttons, and the keyboard focus and pointer captures inside the screen are released. A
    /// screen under another one stays visible and still catches the pointer: clicks on it reach neither its buttons nor
    /// whatever is behind it.
    /// </summary>
    internal sealed class ScreenInputBlocker
    {
        private readonly VisualElement _root;

        public ScreenInputBlocker(VisualElement root)
        {
            _root = root;

            _root.RegisterCallback<PointerDownEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerUpEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerMoveEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerCancelEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<ClickEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<WheelEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<KeyDownEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<NavigationSubmitEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<NavigationMoveEvent>(Block, TrickleDown.TrickleDown);
            _root.RegisterCallback<NavigationCancelEvent>(Block, TrickleDown.TrickleDown);
        }

        public bool IsBlocked { get; private set; }

        public void SetBlocked(bool isBlocked)
        {
            IsBlocked = isBlocked;

            if (isBlocked)
            {
                ReleaseFocusAndPointers();
            }
        }

        private void Block(EventBase evt)
        {
            if (!IsBlocked)
            {
                return;
            }

            evt.StopPropagation();
            _root.focusController?.IgnoreEvent(evt);
        }

        private void ReleaseFocusAndPointers()
        {
            if (_root.focusController?.focusedElement is VisualElement focused && Contains(focused))
            {
                focused.Blur();
            }

            var panel = _root.panel;

            if (panel == null)
            {
                return;
            }

            for (var pointerId = 0; pointerId < PointerId.maxPointers; pointerId++)
            {
                if (panel.GetCapturingElement(pointerId) is VisualElement capturing && Contains(capturing))
                {
                    capturing.ReleasePointer(pointerId);
                }
            }
        }

        private bool Contains(VisualElement element)
        {
            return element == _root || _root.Contains(element);
        }
    }
}
