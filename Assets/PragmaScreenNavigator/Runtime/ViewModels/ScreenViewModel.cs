namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The view model of a screen: the data and actions of the screen, without access to the view. The navigation creates it
    /// through <see cref="IViewModelFactory"/> (dependencies come through the constructor), once per screen instance. A closed
    /// screen goes to the pool together with its view model and opens again with the same one, so everything that lives from
    /// show to hide (subscriptions, timers) starts in <see cref="OnShow"/> and ends in <see cref="OnHide"/>. Open data
    /// arrives through <see cref="IScreenArgs{TArgs}"/>.
    /// </summary>
    public abstract class ScreenViewModel : ViewModel
    {
        private IScreenOwner _owner;

        public ScreenState State { get; private set; }

        /// <summary>Created and bound to the view, once in the life of the screen instance.</summary>
        protected internal virtual void OnCreate()
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

        /// <summary>Every frame while the screen is visible, focused or under another screen.</summary>
        protected internal virtual void OnUpdate()
        {
        }

        /// <summary>Back reached the focused screen: true lets the navigation close it, false keeps it open.</summary>
        protected internal virtual bool OnBack()
        {
            return true;
        }

        /// <summary>The screen instance is destroyed: its pool dropped it, or the navigation was disposed.</summary>
        protected internal virtual void OnDestroy()
        {
        }

        /// <summary>
        /// Closes this screen: the top one like <see cref="INavigation{TScreen}.Pop"/>, one in the middle of the history
        /// without touching the screens above it.
        /// </summary>
        protected void Close()
        {
            _owner?.Close(this);
        }

        internal void SetOwner(IScreenOwner owner)
        {
            _owner = owner;
        }

        internal void SetState(ScreenState state)
        {
            State = state;
        }
    }
}
