using System;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// <see cref="INavigation{TScreen}"/> over a UI Toolkit root (<see cref="Root"/>): screens are views cloned from the UXML
    /// of the catalog (<see cref="ScreenCatalog{TScreen}"/>) with view models from <see cref="IViewModelFactory"/>. Screen
    /// instances are pooled by id and element view models by type, in pools from <see cref="IInstancePoolFactory"/>: a
    /// closed screen, once its hide animation ends, stays hidden in the root and opens again as the same instance.
    ///
    /// The screens of the history are visible down to the topmost <see cref="ScreenMode.Page"/>: a page hides the screens
    /// below it once its show animation ends, and shows them again in their places when it leaves the history.
    ///
    /// The navigation creates its root itself, and whoever holds the navigation adds the root to a document
    /// (<see cref="NavigationHost{TScreen,TCatalog}"/>), so the navigation can be created before the document has a panel.
    /// <see cref="Update"/> passes the frame to the visible screens and has to be called every frame. <see cref="Dispose"/>
    /// hides the open screens and destroys all of them.
    /// </summary>
    public sealed class Navigation<TScreen> : INavigation<TScreen>, IScreenOwner, IDisposable where TScreen : struct, Enum
    {
        public const string ROOT_NAME = "navigation-root";

        private readonly ScreenCatalog<TScreen> _catalog;
        private readonly IViewModelFactory _factory;
        private readonly IInstancePoolFactory _poolFactory;
        private readonly ElementViewModelPool _elementViewModels;
        private readonly Action<Screen> _destroy;
        private readonly Dictionary<TScreen, IInstancePool<Screen>> _pools = new Dictionary<TScreen, IInstancePool<Screen>>();
        private readonly List<Screen> _history = new List<Screen>();
        private readonly List<Screen> _closing = new List<Screen>();

        private bool _isDisposed;
        private bool _isUpdatingCover;
        private bool _isCoverDirty;

        /// <param name="pools">The pools of screen instances and element view models; <see cref="StackInstancePoolFactory"/> when null.</param>
        public Navigation(ScreenCatalog<TScreen> catalog, IViewModelFactory factory, IInstancePoolFactory pools = null)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _catalog = catalog;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _poolFactory = pools ?? new StackInstancePoolFactory();
            _elementViewModels = new ElementViewModelPool(factory, _poolFactory);
            _destroy = Destroy;

            Root = new VisualElement { name = ROOT_NAME, pickingMode = PickingMode.Ignore };
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.top = 0;
            Root.style.right = 0;
            Root.style.bottom = 0;
        }

        /// <summary>The root of the screens: stretched over its parent, its empty space lets the pointer through.</summary>
        public VisualElement Root { get; }

        private Screen Top => _history.Count > 0 ? _history[_history.Count - 1] : null;

        public bool TryGetCurrent(out TScreen screen)
        {
            var top = Top;
            screen = top?.id ?? default;

            return top != null;
        }

        public bool IsOpen(TScreen screen)
        {
            return LastIndexOf(screen) >= 0;
        }

        public void Push(TScreen screen)
        {
            Open(Rent(screen));
        }

        public void Push<TArgs>(TScreen screen, TArgs args)
        {
            Open(Rent(screen, args));
        }

        public void Replace(TScreen screen)
        {
            ReplaceTop(Rent(screen));
        }

        public void Replace<TArgs>(TScreen screen, TArgs args)
        {
            ReplaceTop(Rent(screen, args));
        }

        public void Pop()
        {
            ThrowIfDisposed();

            if (_history.Count == 0)
            {
                return;
            }

            var top = Top;
            _history.RemoveAt(_history.Count - 1);
            Close(top);
            UpdateCover();
            FocusTop();
        }

        public void PopTo(TScreen screen)
        {
            ThrowIfDisposed();

            var index = LastIndexOf(screen);

            if (index < 0 || index == _history.Count - 1)
            {
                return;
            }

            CloseAbove(index);
            UpdateCover();
            FocusTop();
        }

        public void PopAll()
        {
            ThrowIfDisposed();
            CloseAbove(-1);
        }

        public bool Back()
        {
            ThrowIfDisposed();

            var top = Top;

            if (top == null || top.view.State != ScreenState.Focused || !top.viewModel.OnBack())
            {
                return false;
            }

            // The view model may have closed its screen itself in OnBack.
            if (Top == top)
            {
                Pop();
            }

            return true;
        }

        /// <summary>Passes the frame to the visible screens of the history, bottom to top, and to their elements.</summary>
        public void Update()
        {
            ThrowIfDisposed();

            if (_history.Count == 0)
            {
                return;
            }

            // A snapshot: a view model may open or close screens in OnUpdate.
            var screens = ListPool<Screen>.Get();
            screens.AddRange(_history);

            foreach (var screen in screens)
            {
                if (screen.view.State != ScreenState.Hidden && _history.Contains(screen))
                {
                    screen.view.UpdateScreen();
                }
            }

            ListPool<Screen>.Release(screens);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            // Every show is paired with a hide: view models drop their subscriptions in OnHide.
            for (var i = _history.Count - 1; i >= 0; i--)
            {
                Destroy(_history[i]);
            }

            foreach (var screen in _closing)
            {
                Destroy(screen);
            }

            _history.Clear();
            _closing.Clear();

            // The pools hold hidden screens and destroy them through the destroy callback.
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
            _elementViewModels.Dispose();
            Root.RemoveFromHierarchy();
        }

        void IScreenOwner.Close(ScreenViewModel viewModel)
        {
            ThrowIfDisposed();

            var index = _history.FindIndex(screen => screen.viewModel == viewModel);

            if (index < 0)
            {
                return;
            }

            if (index == _history.Count - 1)
            {
                Pop();
                return;
            }

            var screen = _history[index];
            _history.RemoveAt(index);
            Close(screen);
            UpdateCover();
        }

        private Screen Rent(TScreen id)
        {
            ThrowIfDisposed();

            return GetPool(id).Get();
        }

        private Screen Rent<TArgs>(TScreen id, TArgs args)
        {
            var screen = Rent(id);

            if (!(screen.viewModel is IScreenArgs<TArgs> receiver))
            {
                GetPool(id).Release(screen);

                throw new InvalidOperationException(
                    $"The view model of screen {id} ({screen.viewModel.GetType().Name}) does not accept {typeof(TArgs).Name}: it needs {nameof(IScreenArgs<TArgs>)}<{typeof(TArgs).Name}>.");
            }

            try
            {
                receiver.SetArgs(args);
            }
            catch
            {
                GetPool(id).Release(screen);
                throw;
            }

            return screen;
        }

        private IInstancePool<Screen> GetPool(TScreen id)
        {
            if (!_pools.TryGetValue(id, out var pool))
            {
                pool = _poolFactory.Create(() => CreateScreen(id), _destroy);
                _pools.Add(id, pool);
            }

            return pool;
        }

        private void Open(Screen screen)
        {
            var previous = Top;
            _history.Add(screen);

            if (previous != null && previous.view.State == ScreenState.Focused)
            {
                previous.view.BlurScreen();
            }

            Show(screen, isBroughtToFront: true);
        }

        private void ReplaceTop(Screen screen)
        {
            if (_history.Count == 0)
            {
                Open(screen);
                return;
            }

            var previous = Top;
            _history[_history.Count - 1] = screen;
            Close(previous);
            Show(screen, isBroughtToFront: true);
            UpdateCover();
        }

        private void CloseAbove(int index)
        {
            for (var i = _history.Count - 1; i > index; i--)
            {
                var screen = _history[i];
                _history.RemoveAt(i);
                Close(screen);
            }
        }

        /// <summary>An opened screen goes on top of the root; an uncovered one shows in its place, below the screens opened after it.</summary>
        private void Show(Screen screen, bool isBroughtToFront)
        {
            if (isBroughtToFront)
            {
                screen.view.BringToFront();
            }

            screen.isShown = false;
            var transition = ++screen.transition;

            screen.view.ShowScreen();
            screen.view.AnimateShow(() => OnShown(screen, transition));
        }

        /// <summary>The show animation ended: a page now covers the screens below it, and the top screen gets the focus.</summary>
        private void OnShown(Screen screen, int transition)
        {
            if (_isDisposed || screen.transition != transition)
            {
                return;
            }

            screen.isShown = true;
            UpdateCover();
            FocusTop();
        }

        private void FocusTop()
        {
            var top = Top;

            if (top != null && top.isShown && top.view.State == ScreenState.Visible)
            {
                top.view.FocusScreen();
            }
        }

        /// <summary>
        /// Hides the screens under a page that has finished showing, and shows again the covered screens that no page of the
        /// history is above any more. A page that is still showing keeps covered what it will cover, so replacing one page
        /// with another does not show the screens below for the time of the animation.
        /// </summary>
        private void UpdateCover()
        {
            // Showing and hiding call view models, which may change the history: the pass then runs again.
            if (_isUpdatingCover)
            {
                _isCoverDirty = true;
                return;
            }

            _isUpdatingCover = true;

            try
            {
                do
                {
                    _isCoverDirty = false;
                    ApplyCover();
                }
                while (_isCoverDirty && !_isDisposed);
            }
            finally
            {
                _isUpdatingCover = false;
            }
        }

        private void ApplyCover()
        {
            var shownPage = -1;
            var topPage = -1;

            for (var i = _history.Count - 1; i >= 0 && shownPage < 0; i--)
            {
                var screen = _history[i];

                if (screen.mode != ScreenMode.Page)
                {
                    continue;
                }

                if (topPage < 0)
                {
                    topPage = i;
                }

                if (screen.isShown)
                {
                    shownPage = i;
                }
            }

            var screens = ListPool<Screen>.Get();
            screens.AddRange(_history);

            for (var i = shownPage - 1; i >= 0; i--)
            {
                var screen = screens[i];

                if (!screen.isCovered && _history.Contains(screen))
                {
                    Cover(screen);
                }
            }

            for (var i = Math.Max(topPage, 0); i < screens.Count; i++)
            {
                var screen = screens[i];

                if (screen.isCovered && _history.Contains(screen))
                {
                    screen.isCovered = false;
                    Show(screen, isBroughtToFront: false);
                }
            }

            ListPool<Screen>.Release(screens);
        }

        /// <summary>A page above has finished showing: the screen hides but stays in the history.</summary>
        private static void Cover(Screen screen)
        {
            screen.isCovered = true;
            screen.isShown = false;
            screen.transition++;

            if (screen.view.State == ScreenState.Focused)
            {
                screen.view.BlurScreen();
            }

            if (screen.view.State == ScreenState.Visible)
            {
                screen.view.HideScreen();
            }
        }

        /// <summary>
        /// The screen is already out of the history: it loses the focus, plays its hide animation, hides and returns to the
        /// pool. A covered screen is hidden already and returns at once.
        /// </summary>
        private void Close(Screen screen)
        {
            screen.isShown = false;
            screen.transition++;

            if (screen.isCovered)
            {
                screen.isCovered = false;
                GetPool(screen.id).Release(screen);
                return;
            }

            if (screen.view.State == ScreenState.Focused)
            {
                screen.view.BlurScreen();
            }

            _closing.Add(screen);
            screen.view.AnimateHide(() => FinishClose(screen));
        }

        private void FinishClose(Screen screen)
        {
            // The navigation was disposed in the middle of the animation: the screen is already destroyed.
            if (!_closing.Remove(screen))
            {
                return;
            }

            if (screen.view.State != ScreenState.Hidden)
            {
                screen.view.HideScreen();
            }

            GetPool(screen.id).Release(screen);
        }

        private void Destroy(Screen screen)
        {
            if (screen.view.State == ScreenState.Focused)
            {
                screen.view.BlurScreen();
            }

            if (screen.view.State == ScreenState.Visible)
            {
                screen.view.HideScreen();
            }

            screen.view.DestroyScreen();
        }

        private Screen CreateScreen(TScreen id)
        {
            if (!_catalog.TryGetEntry(id, out var entry))
            {
                throw new InvalidOperationException($"Screen {id} has no UXML in the catalog {_catalog.name}.");
            }

            var view = Instantiate(entry.View, id);

            if (!(_factory.Create(view.ViewModelType) is ScreenViewModel viewModel) || !view.ViewModelType.IsInstanceOfType(viewModel))
            {
                throw new InvalidOperationException($"The view model factory did not create {view.ViewModelType.Name} for screen {id}.");
            }

            viewModel.SetOwner(this);

            try
            {
                view.CreateScreen(viewModel, _elementViewModels);
            }
            catch
            {
                // The cleanup must not hide the exception that broke the screen.
                try
                {
                    view.DestroyScreen();
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }

                throw;
            }

            // Into the root only after creation: elements register when the screen attaches to a panel, and they need the pool by then.
            Root.Add(view);

            return new Screen(id, entry.Mode, view, viewModel);
        }

        /// <summary>Clones the UXML and takes its root as the screen view; the style sheets of the UXML root sit on the discarded container.</summary>
        private static ScreenView Instantiate(VisualTreeAsset asset, TScreen id)
        {
            var container = asset.Instantiate();

            foreach (var child in container.Children())
            {
                if (!(child is ScreenView view))
                {
                    continue;
                }

                for (var i = 0; i < container.styleSheets.count; i++)
                {
                    view.styleSheets.Add(container.styleSheets[i]);
                }

                view.RemoveFromHierarchy();

                return view;
            }

            throw new InvalidOperationException($"The root of UXML '{asset.name}' of screen {id} is not a screen view ({nameof(ScreenView)}).");
        }

        private int LastIndexOf(TScreen id)
        {
            for (var i = _history.Count - 1; i >= 0; i--)
            {
                if (EqualityComparer<TScreen>.Default.Equals(_history[i].id, id))
                {
                    return i;
                }
            }

            return -1;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(Navigation<TScreen>));
            }
        }

        /// <summary>A screen instance: the view and the view model live and are pooled together.</summary>
        private sealed class Screen
        {
            public readonly TScreen id;
            public readonly ScreenMode mode;
            public readonly ScreenView view;
            public readonly ScreenViewModel viewModel;

            /// <summary>The show animation has ended since the screen was last shown.</summary>
            public bool isShown;

            /// <summary>Hidden under a page while staying in the history.</summary>
            public bool isCovered;

            /// <summary>Changes on every show, cover and close, so a show animation that ends late is ignored.</summary>
            public int transition;

            public Screen(TScreen id, ScreenMode mode, ScreenView view, ScreenViewModel viewModel)
            {
                this.id = id;
                this.mode = mode;
                this.view = view;
                this.viewModel = viewModel;
            }
        }
    }
}
