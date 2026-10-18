namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The view model of a screen element (<see cref="ScreenElement{TViewModel}"/>). <see cref="IViewModelFactory"/> creates
    /// it, and it lives in a pool per type: an element that lands on a screen takes a view model from the pool, an element
    /// that leaves returns it, so the state is reset in <see cref="OnUnbind"/>. It follows the lifecycle of the screen its
    /// element is on: an element added to a visible screen catches up right away (<see cref="OnShow"/>, then
    /// <see cref="OnFocus"/>).
    /// </summary>
    public abstract class ElementViewModel : ViewModel
    {
        /// <summary>Bound to an element, before the element makes it its dataSource.</summary>
        protected internal virtual void OnBind()
        {
        }

        /// <summary>Unbound from the element and going back to the pool: reset the state here.</summary>
        protected internal virtual void OnUnbind()
        {
        }

        protected internal virtual void OnShow()
        {
        }

        protected internal virtual void OnFocus()
        {
        }

        protected internal virtual void OnBlur()
        {
        }

        protected internal virtual void OnHide()
        {
        }

        /// <summary>Every frame while the screen of the element is visible.</summary>
        protected internal virtual void OnUpdate()
        {
        }

        /// <summary>Destroyed while sleeping in the pool: the pool dropped it or the navigation was disposed.</summary>
        protected internal virtual void OnDestroy()
        {
        }
    }
}
