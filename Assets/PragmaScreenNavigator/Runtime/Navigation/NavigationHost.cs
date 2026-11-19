using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The navigation of a scene on a UI Toolkit document: creates the navigation with the view model factory it is given
    /// (<see cref="CreateNavigation"/>), adds its root to the document and opens the start screen on Start, passes it the
    /// frame every Update and disposes it in OnDestroy. The document part — the root in the document, the safe area, the
    /// frame — is <see cref="DocumentNavigation{TScreen}"/>, which a document created from code uses without a host.
    ///
    /// The host knows no DI container. A concrete host is a subclass with its own screen enum and catalog, and it calls
    /// <see cref="CreateNavigation"/> before Start: from the registration in its container, with a factory over that
    /// container, so the view models get their dependencies from the scene scope; or from Awake with
    /// <see cref="ActivatorViewModelFactory"/> when the scene has no container. Whatever else the scene needs (a Back key,
    /// pointer over UI checks) the subclass adds in <see cref="Start"/> and <see cref="Update"/>.
    /// </summary>
    public abstract class NavigationHost<TScreen, TCatalog> : MonoBehaviour
        where TScreen : struct, Enum
        where TCatalog : ScreenCatalog<TScreen>
    {
        [SerializeField, Tooltip("The UI Toolkit document the screens are shown in: its panel and theme are shared by all screens.")]
        private UIDocument _document;

        [SerializeField, Tooltip("The screen catalog: which UXML each screen has.")]
        private TCatalog _screens;

        [SerializeField, Tooltip("The screen opened when the scene starts.")]
        private TScreen _startScreen;

        [SerializeField, Tooltip("Keep the screens inside the screen's safe area (a camera cutout, rounded corners); the world under the document stays on the whole screen.")]
        private bool _isSafeAreaInset = true;

        private DocumentNavigation<TScreen> _host;

        protected INavigation<TScreen> Navigation => _host?.Navigation;

        protected UIDocument Document => _document;

        /// <summary>The screen opened on start; a subclass picks another one when the scene has a choice.</summary>
        protected virtual TScreen StartScreen => _startScreen;

        /// <summary>
        /// Creates the navigation of the scene with view models from <paramref name="factory"/>, once and before Start. Returns
        /// it, so a DI integration can register it in its container.
        /// </summary>
        /// <param name="pools">The pools of screen instances and element view models; <see cref="StackInstancePoolFactory"/> when null.</param>
        protected Navigation<TScreen> CreateNavigation(IViewModelFactory factory, IInstancePoolFactory pools = null)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (_document == null || _screens == null)
            {
                throw new InvalidOperationException($"{GetType().Name}: the document or the screen catalog is not assigned.");
            }

            if (_host != null)
            {
                throw new InvalidOperationException($"{GetType().Name}: the navigation is already created.");
            }

            _host = new DocumentNavigation<TScreen>(_document, _screens, factory, pools, _isSafeAreaInset);

            return _host.Navigation;
        }

        /// <summary>The document creates its root in OnEnable, so it exists by Start.</summary>
        protected virtual void Start()
        {
            if (_host == null)
            {
                throw new InvalidOperationException($"{GetType().Name}: the navigation is not created, call {nameof(CreateNavigation)} before Start.");
            }

            _host.Attach();
            _host.Navigation.Push(StartScreen);
        }

        protected virtual void Update()
        {
            _host?.Update();
        }

        /// <summary>Dispose is idempotent, so a container that disposes the navigation as well does no harm.</summary>
        protected virtual void OnDestroy()
        {
            _host?.Dispose();
        }
    }
}
