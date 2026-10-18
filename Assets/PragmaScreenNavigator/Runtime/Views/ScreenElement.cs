using System;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// A part of a screen with its own view model, an element of a composite screen: resources, a minimap, a list row.
    /// Declared as a <c>[UxmlElement] partial</c> subclass of <see cref="ScreenElement{TViewModel}"/> and placed in the
    /// screen UXML or added at run time. Attached to a panel, it registers on the nearest screen above it and gets a view
    /// model from the pool; detached, it leaves the screen and returns the view model. Moving it to another parent is a
    /// leave and a new registration as well.
    /// </summary>
    public abstract class ScreenElement : VisualElement
    {
        private ScreenView _screen;

        protected ScreenElement()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        internal abstract Type ViewModelType { get; }

        internal abstract void Bind(ElementViewModel viewModel);

        internal abstract void Unbind();

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var screen = FindScreen();

            if (screen == _screen)
            {
                return;
            }

            Leave();

            _screen = screen;
            _screen?.Elements.Register(this);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Leave();
        }

        private void Leave()
        {
            if (_screen == null)
            {
                return;
            }

            var screen = _screen;
            _screen = null;
            screen.Elements.Deregister(this);
        }

        private ScreenView FindScreen()
        {
            for (var element = hierarchy.parent; element != null; element = element.hierarchy.parent)
            {
                if (element is ScreenView screen)
                {
                    return screen;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// A screen element with the view model <typeparamref name="TViewModel"/>: the view model is the dataSource of the
    /// element, and bindings inside the element resolve against it. View models are pooled: unsubscribe from it in
    /// <see cref="OnUnbind"/>.
    /// </summary>
    public abstract class ScreenElement<TViewModel> : ScreenElement where TViewModel : ElementViewModel
    {
        protected TViewModel ViewModel { get; private set; }

        internal override Type ViewModelType => typeof(TViewModel);

        /// <summary>The view model became the dataSource: subscribe buttons to its methods, pass the element settings from UXML to it.</summary>
        protected virtual void OnBind(TViewModel viewModel)
        {
        }

        protected virtual void OnUnbind(TViewModel viewModel)
        {
        }

        /// <summary>
        /// A view model property changed: update here what UXML bindings do not cover, such as visibility, classes, list rows
        /// and drawing.
        /// </summary>
        protected virtual void OnViewModelChanged(string property)
        {
        }

        internal override void Bind(ElementViewModel viewModel)
        {
            ViewModel = (TViewModel)viewModel;
            dataSource = ViewModel;
            ViewModel.propertyChanged += OnPropertyChanged;
            OnBind(ViewModel);
        }

        internal override void Unbind()
        {
            var viewModel = ViewModel;

            if (viewModel == null)
            {
                return;
            }

            viewModel.propertyChanged -= OnPropertyChanged;
            OnUnbind(viewModel);
            ViewModel = null;
            dataSource = null;
        }

        private void OnPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            OnViewModelChanged(args.propertyName);
        }
    }
}
