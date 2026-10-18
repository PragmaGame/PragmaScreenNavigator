using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>The root of Screens/FirstTestScreen.uxml: a counter element in the markup and a container for elements added by the test.</summary>
    [UxmlElement]
    public partial class FirstTestScreen : ScreenView<FirstTestViewModel>
    {
        public VisualElement List { get; private set; }

        public List<string> Changes { get; } = new List<string>();

        protected override void OnCreate()
        {
            List = this.Q("list");
        }

        protected override void OnViewModelChanged(string property)
        {
            Changes.Add(property);
        }
    }

    [UxmlElement]
    public partial class SecondTestScreen : ScreenView<SecondTestViewModel>
    {
    }

    [UxmlElement]
    public partial class DialogTestScreen : ScreenView<DialogTestViewModel>
    {
    }

    [UxmlElement]
    public partial class PageTestScreen : ScreenView<PageTestViewModel>
    {
    }

    /// <summary>Fails while the screen is created, after its view model has run OnCreate.</summary>
    [UxmlElement]
    public partial class BrokenTestScreen : ScreenView<BrokenTestViewModel>
    {
        protected override void OnBind(BrokenTestViewModel viewModel)
        {
            throw new InvalidOperationException("The view failed to bind.");
        }
    }

    /// <summary>The show and hide animations wait until the test calls their completion.</summary>
    [UxmlElement]
    public partial class AnimatedTestScreen : ScreenView<AnimatedTestViewModel>
    {
        public Action PendingShow { get; private set; }

        public Action PendingHide { get; private set; }

        protected override void AnimateShow(Action completed)
        {
            PendingShow = completed;
        }

        protected override void AnimateHide(Action completed)
        {
            PendingShow = null;
            PendingHide = completed;
        }
    }

    [UxmlElement]
    public partial class CounterElement : ScreenElement<CounterViewModel>
    {
        public CounterViewModel BoundViewModel => ViewModel;

        public List<string> Changes { get; } = new List<string>();

        protected override void OnViewModelChanged(string property)
        {
            Changes.Add(property);
        }
    }
}
