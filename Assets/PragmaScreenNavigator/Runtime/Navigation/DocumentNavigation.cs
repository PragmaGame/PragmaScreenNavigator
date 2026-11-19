using System;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The navigation of screens shown in a UI Toolkit document, hosted from code: what <see cref="NavigationHost{TScreen,TCatalog}"/>
    /// does in a scene, for a document created at run time — a loading window, a console, a screen layer that outlives
    /// scenes. It creates the navigation and owns it, puts its root into the document whenever the document has a root
    /// without it (a document builds its root in OnEnable and builds a new one after it is enabled again), keeps the root
    /// inside the screen's safe area (<see cref="SafeAreaInset"/>, when asked) and passes the navigation the frame. The
    /// owner calls <see cref="Update"/> every frame and <see cref="Dispose"/> when the screens go; which screens to open is
    /// up to it.
    /// </summary>
    public sealed class DocumentNavigation<TScreen> : IDisposable
        where TScreen : struct, Enum
    {
        private readonly UIDocument _document;
        private readonly Navigation<TScreen> _navigation;
        private readonly SafeAreaInset _safeArea;

        /// <param name="document">The document the screens are shown in: its panel and theme are shared by all screens.</param>
        /// <param name="screens">The screen catalog: which UXML each screen has.</param>
        /// <param name="viewModels">Where the view models of the screens come from.</param>
        /// <param name="pools">The pools of screen instances and element view models; <see cref="StackInstancePoolFactory"/> when null.</param>
        /// <param name="isSafeAreaInset">
        /// Keep the screens inside the screen's safe area. Off for screens that must cover the whole screen, a cutout too, and
        /// keep their content away from its edges themselves.
        /// </param>
        public DocumentNavigation(UIDocument document, ScreenCatalog<TScreen> screens, IViewModelFactory viewModels, IInstancePoolFactory pools = null,
                                  bool isSafeAreaInset = true)
        {
            _document = document != null ? document : throw new ArgumentNullException(nameof(document));
            _navigation = new Navigation<TScreen>(screens, viewModels, pools);
            _safeArea = isSafeAreaInset ? new SafeAreaInset(_navigation.Root) : null;
        }

        public Navigation<TScreen> Navigation => _navigation;

        public UIDocument Document => _document;

        /// <summary>The navigation root is in the document's current root.</summary>
        public bool IsAttached => _document != null && _document.rootVisualElement != null && _navigation.Root.parent == _document.rootVisualElement;

        /// <summary>
        /// Puts the navigation root into the document's root, if the document has one and the navigation root is not in it yet.
        /// Empty space of the document lets the pointer through, as do the navigation root and the screens: only panels and
        /// buttons catch it.
        /// </summary>
        /// <returns>The navigation root is in the document.</returns>
        public bool Attach()
        {
            var root = _document != null ? _document.rootVisualElement : null;

            if (root == null)
            {
                return false;
            }

            if (_navigation.Root.parent != root)
            {
                root.pickingMode = PickingMode.Ignore;
                root.Add(_navigation.Root);
            }

            return true;
        }

        /// <summary>The frame: attaches the root if the document has built a new one, keeps the safe area, updates the navigation.</summary>
        public void Update()
        {
            if (Attach())
            {
                _safeArea?.Update();
            }

            _navigation.Update();
        }

        /// <summary>Disposes the navigation with its screens; idempotent.</summary>
        public void Dispose()
        {
            _navigation.Dispose();
        }
    }
}
