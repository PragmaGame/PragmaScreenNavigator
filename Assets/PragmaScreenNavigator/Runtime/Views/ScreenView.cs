using System;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// A UI Toolkit screen view, the root of its UXML: a concrete screen is a <c>[UxmlElement] partial</c> subclass of
    /// <see cref="ScreenView{TViewModel}"/>. It stretches over the whole navigation root and does not catch the pointer
    /// itself: empty space of the screen lets clicks through to whatever is below. Out of focus its buttons get no input.
    ///
    /// The navigation drives the lifecycle: show and focus go to the view, then the view model, then the elements; blur and
    /// hide go in reverse order.
    /// </summary>
    public abstract class ScreenView : VisualElement
    {
        public const string USS_CLASS_NAME = "screen";

        private readonly ScreenInputBlocker _inputBlocker;
        private readonly ScreenElements _elements = new ScreenElements();

        private ScreenViewModel _viewModel;

        protected ScreenView()
        {
            AddToClassList(USS_CLASS_NAME);

            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Ignore;

            _inputBlocker = new ScreenInputBlocker(this);
        }

        public ScreenState State { get; private set; }

        internal ScreenElements Elements => _elements;

        internal abstract Type ViewModelType { get; }

        /// <summary>The UXML is cloned: query the controls here (Q). There is no view model yet.</summary>
        protected virtual void OnCreate()
        {
        }

        protected virtual void OnShow()
        {
        }

        protected virtual void OnFocus()
        {
        }

        protected virtual void OnBlur()
        {
        }

        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// Show animation: the screen is already visible and gets the focus once the animation ends and calls
        /// <paramref name="completed"/>.
        /// </summary>
        protected internal virtual void AnimateShow(Action completed)
        {
            completed();
        }

        /// <summary>
        /// Hide animation: the screen has already lost the focus; it hides and returns to the pool on
        /// <paramref name="completed"/>. It may start in the middle of the show animation, which then has to be stopped.
        /// </summary>
        protected internal virtual void AnimateHide(Action completed)
        {
            completed();
        }

        private protected abstract void BindViewModel(ScreenViewModel viewModel);

        private protected abstract void UnbindViewModel();

        /// <summary>
        /// The navigation created the screen, which is not in its root yet: elements that register on the screen take view
        /// models from <paramref name="pool"/>.
        /// </summary>
        internal void CreateScreen(ScreenViewModel viewModel, ElementViewModelPool pool)
        {
            style.display = DisplayStyle.None;
            _inputBlocker.SetBlocked(true);
            _elements.SetPool(pool);

            OnCreate();
            viewModel.OnCreate();

            // Assigned only after OnCreate, so a view model that failed in it does not get OnDestroy.
            _viewModel = viewModel;
            dataSource = viewModel;
            BindViewModel(viewModel);
        }

        internal void ShowScreen()
        {
            SetState(ScreenState.Visible);
            style.display = DisplayStyle.Flex;

            OnShow();
            _viewModel.OnShow();
            _elements.Show();
        }

        internal void FocusScreen()
        {
            SetState(ScreenState.Focused);
            _inputBlocker.SetBlocked(false);

            OnFocus();
            _viewModel.OnFocus();
            _elements.Focus();
        }

        internal void BlurScreen()
        {
            SetState(ScreenState.Visible);
            _inputBlocker.SetBlocked(true);

            _elements.Blur();
            _viewModel.OnBlur();
            OnBlur();
        }

        internal void HideScreen()
        {
            SetState(ScreenState.Hidden);

            _elements.Hide();
            _viewModel.OnHide();
            OnHide();

            style.display = DisplayStyle.None;
        }

        internal void UpdateScreen()
        {
            _viewModel.OnUpdate();
            _elements.Update();
        }

        /// <summary>
        /// Leaves the root, so the elements return their view models to the pool, then unbinds and destroys the view model.
        /// Also cleans up a screen whose <see cref="CreateScreen"/> failed half way.
        /// </summary>
        internal void DestroyScreen()
        {
            RemoveFromHierarchy();

            UnbindViewModel();
            dataSource = null;

            if (_viewModel == null)
            {
                return;
            }

            _viewModel.OnDestroy();
            _viewModel.SetOwner(null);
        }

        private void SetState(ScreenState state)
        {
            State = state;
            _viewModel.SetState(state);
        }
    }

    /// <summary>
    /// A screen view with the view model <typeparamref name="TViewModel"/>: the view model is the dataSource of the screen,
    /// UXML bindings read its [CreateProperty] properties, and <see cref="OnBind"/> subscribes buttons to its methods.
    /// </summary>
    public abstract class ScreenView<TViewModel> : ScreenView where TViewModel : ScreenViewModel
    {
        protected TViewModel ViewModel { get; private set; }

        internal override Type ViewModelType => typeof(TViewModel);

        /// <summary>The view model became the dataSource: subscribe buttons to its methods and the view to its events.</summary>
        protected virtual void OnBind(TViewModel viewModel)
        {
        }

        protected virtual void OnUnbind(TViewModel viewModel)
        {
        }

        /// <summary>
        /// A view model property changed: update here what UXML bindings do not cover, such as visibility, classes and list
        /// rows.
        /// </summary>
        protected virtual void OnViewModelChanged(string property)
        {
        }

        private protected override void BindViewModel(ScreenViewModel viewModel)
        {
            ViewModel = (TViewModel)viewModel;
            ViewModel.propertyChanged += OnPropertyChanged;
            OnBind(ViewModel);
        }

        private protected override void UnbindViewModel()
        {
            var viewModel = ViewModel;

            if (viewModel == null)
            {
                return;
            }

            ViewModel = null;
            viewModel.propertyChanged -= OnPropertyChanged;
            OnUnbind(viewModel);
        }

        private void OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            OnViewModelChanged(args.propertyName);
        }
    }
}
