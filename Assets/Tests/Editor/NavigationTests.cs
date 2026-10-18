using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>
    /// <see cref="Navigation{TScreen}"/> on the test screens (Screens/*.uxml): history and focus, the pool of screen
    /// instances, open arguments, Back, animations, screen elements with pooled view models and disposal. View models come
    /// from <see cref="TestViewModelFactory"/> with the <see cref="TestLog"/>. Elements register on a screen only while it is
    /// in a panel, so those tests put the navigation root into a UIDocument.
    /// </summary>
    public class NavigationTests
    {
        private TestLog _log;
        private TestScreens _catalog;
        private Navigation<TestScreen> _navigation;

        private GameObject _panelHost;
        private PanelSettings _panelSettings;

        [SetUp]
        public void SetUp()
        {
            _log = new TestLog();
            _catalog = CreateCatalog();
            _navigation = new Navigation<TestScreen>(_catalog, new TestViewModelFactory(_log));
        }

        [TearDown]
        public void TearDown()
        {
            _navigation.Dispose();
            Object.DestroyImmediate(_catalog);

            if (_panelHost != null)
            {
                Object.DestroyImmediate(_panelHost);
            }

            if (_panelSettings != null)
            {
                Object.DestroyImmediate(_panelSettings);
            }
        }

        [Test]
        public void Push_ShowsTheScreenAndFocusesIt()
        {
            _navigation.Push(TestScreen.First);

            CollectionAssert.AreEqual(new[] { "First.Create", "First.Show", "First.Focus" }, _log.Take());
            Assert.IsTrue(_navigation.TryGetCurrent(out var current));
            Assert.AreEqual(TestScreen.First, current);
            Assert.AreEqual(ScreenState.Focused, ViewModel<FirstTestViewModel>().State);
            Assert.AreEqual(DisplayStyle.Flex, View<FirstTestScreen>().style.display.value);
        }

        [Test]
        public void Push_BlursTheScreenBelow_AndPopFocusesItAgain()
        {
            _navigation.Push(TestScreen.First);
            _log.Take();

            _navigation.Push(TestScreen.Second);

            CollectionAssert.AreEqual(new[] { "Second.Create", "First.Blur", "Second.Show", "Second.Focus" }, _log.Take());
            Assert.AreEqual(ScreenState.Visible, ViewModel<FirstTestViewModel>().State, "the screen below stays visible");

            _navigation.Pop();

            CollectionAssert.AreEqual(new[] { "Second.Blur", "Second.Hide", "First.Focus" }, _log.Take());
            Assert.AreEqual(DisplayStyle.None, View<SecondTestScreen>().style.display.value);
            Assert.IsFalse(_navigation.IsOpen(TestScreen.Second));
        }

        [Test]
        public void ClosedScreen_IsReusedFromThePool()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Pop();
            _log.Take();

            _navigation.Push(TestScreen.First);

            CollectionAssert.AreEqual(new[] { "First.Show", "First.Focus" }, _log.Take(), "no new view model");
            Assert.AreEqual(1, _navigation.Root.childCount, "no new view");
        }

        [Test]
        public void SameScreenTwice_GetsTwoInstances()
        {
            _navigation.Push(TestScreen.Dialog, "a");
            _navigation.Push(TestScreen.Dialog, "b");

            var dialogs = _log.Created.OfType<DialogTestViewModel>().ToArray();
            CollectionAssert.AreEqual(new[] { "a", "b" }, dialogs.Select(dialog => dialog.Text));
            Assert.AreEqual(ScreenState.Visible, dialogs[0].State);
            Assert.AreEqual(ScreenState.Focused, dialogs[1].State);

            _navigation.Pop();
            _navigation.Pop();
            _navigation.Push(TestScreen.Dialog, "c");

            Assert.AreEqual(2, _log.Created.OfType<DialogTestViewModel>().Count(), "the closed instances are reused");
            Assert.AreEqual(1, dialogs.Count(dialog => dialog.Text == "c" && dialog.State == ScreenState.Focused));
        }

        [Test]
        public void Replace_ClosesTheCurrentScreen()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Second);
            _log.Take();

            _navigation.Replace(TestScreen.Dialog, "x");

            CollectionAssert.AreEqual(
                new[] { "Dialog.Create", "Dialog.Args x", "Second.Blur", "Second.Hide", "Dialog.Show", "Dialog.Focus" }, _log.Take());
            Assert.AreEqual(ScreenState.Visible, ViewModel<FirstTestViewModel>().State);

            _navigation.Pop();

            CollectionAssert.AreEqual(new[] { "Dialog.Blur", "Dialog.Hide", "First.Focus" }, _log.Take());
        }

        [Test]
        public void Replace_OnEmptyHistory_Pushes()
        {
            _navigation.Replace(TestScreen.First);

            CollectionAssert.AreEqual(new[] { "First.Create", "First.Show", "First.Focus" }, _log.Take());
        }

        [Test]
        public void PopTo_ClosesTheScreensAbove_AndPopAllClosesEverything()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Second);
            _navigation.Push(TestScreen.Dialog, "x");
            _log.Take();

            _navigation.PopTo(TestScreen.First);

            CollectionAssert.AreEqual(new[] { "Dialog.Blur", "Dialog.Hide", "Second.Hide", "First.Focus" }, _log.Take());

            _navigation.PopTo(TestScreen.Second);
            Assert.IsEmpty(_log.Take(), "a screen that is not open: nothing happens");

            _navigation.PopAll();

            CollectionAssert.AreEqual(new[] { "First.Blur", "First.Hide" }, _log.Take());
            Assert.IsFalse(_navigation.TryGetCurrent(out _));
        }

        [Test]
        public void Back_ClosesTheFocusedScreen_UnlessItsViewModelKeepsIt()
        {
            Assert.IsFalse(_navigation.Back(), "empty history");

            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Second);
            ViewModel<SecondTestViewModel>().IsClosedByBack = false;
            _log.Take();

            Assert.IsFalse(_navigation.Back());
            CollectionAssert.AreEqual(new[] { "Second.Back" }, _log.Take());

            ViewModel<SecondTestViewModel>().IsClosedByBack = true;

            Assert.IsTrue(_navigation.Back());
            CollectionAssert.AreEqual(new[] { "Second.Back", "Second.Blur", "Second.Hide", "First.Focus" }, _log.Take());
        }

        [Test]
        public void Args_ReachTheViewModelBeforeTheScreenIsShown()
        {
            _navigation.Push(TestScreen.Dialog, "hello");

            CollectionAssert.AreEqual(new[] { "Dialog.Create", "Dialog.Args hello", "Dialog.Show", "Dialog.Focus" }, _log.Take());
        }

        [Test]
        public void Args_ForAScreenWithoutThem_Throw_AndKeepTheHistory()
        {
            _navigation.Push(TestScreen.First);

            Assert.Throws<InvalidOperationException>(() => _navigation.Push(TestScreen.Second, 5));

            Assert.IsTrue(_navigation.TryGetCurrent(out var current));
            Assert.AreEqual(TestScreen.First, current);
            Assert.AreEqual(ScreenState.Focused, ViewModel<FirstTestViewModel>().State);

            _navigation.Push(TestScreen.Second);

            Assert.AreEqual(1, _log.Created.OfType<SecondTestViewModel>().Count(), "the rented instance went back to the pool");
        }

        [Test]
        public void ScreenWithoutUxml_Throws()
        {
            _navigation.Push(TestScreen.First);

            Assert.Throws<InvalidOperationException>(() => _navigation.Push(TestScreen.Missing));
            Assert.IsTrue(_navigation.TryGetCurrent(out var current));
            Assert.AreEqual(TestScreen.First, current);
        }

        [Test]
        public void Close_FromTheMiddle_KeepsTheScreensAbove()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Second);
            _navigation.Push(TestScreen.Dialog, "x");
            _log.Take();

            ViewModel<FirstTestViewModel>().CloseSelf();

            CollectionAssert.AreEqual(new[] { "First.Hide" }, _log.Take());
            Assert.IsFalse(_navigation.IsOpen(TestScreen.First));
            Assert.AreEqual(ScreenState.Focused, ViewModel<DialogTestViewModel>().State);

            ViewModel<DialogTestViewModel>().CloseSelf();

            CollectionAssert.AreEqual(new[] { "Dialog.Blur", "Dialog.Hide", "Second.Focus" }, _log.Take());
        }

        [Test]
        public void Update_ReachesOnlyVisibleScreens()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Second);

            _navigation.Update();

            Assert.AreEqual(1, ViewModel<FirstTestViewModel>().Updates, "the screen below is visible");
            Assert.AreEqual(1, ViewModel<SecondTestViewModel>().Updates);

            _navigation.Pop();
            _navigation.Update();

            Assert.AreEqual(2, ViewModel<FirstTestViewModel>().Updates);
            Assert.AreEqual(1, ViewModel<SecondTestViewModel>().Updates, "a closed screen gets no frames");
        }

        [Test]
        public void Animations_DelayTheFocus_AndTheReturnToThePool()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Animated);
            var view = View<AnimatedTestScreen>();
            var viewModel = ViewModel<AnimatedTestViewModel>();

            Assert.AreEqual(ScreenState.Visible, viewModel.State, "no focus while the show animation plays");

            view.PendingShow();

            Assert.AreEqual(ScreenState.Focused, viewModel.State);

            _navigation.Pop();

            Assert.AreEqual(ScreenState.Visible, viewModel.State, "still visible while the hide animation plays");
            Assert.AreEqual(ScreenState.Focused, ViewModel<FirstTestViewModel>().State);

            _navigation.Push(TestScreen.Animated);

            Assert.AreEqual(2, _log.Created.OfType<AnimatedTestViewModel>().Count(), "the hiding instance is not in the pool yet");

            view.PendingHide();

            Assert.AreEqual(ScreenState.Hidden, viewModel.State);
        }

        [Test]
        public void ShowAnimation_OfAClosedScreen_DoesNotFocusIt()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Animated);
            var view = View<AnimatedTestScreen>();
            var showCompleted = view.PendingShow;

            _navigation.Pop();
            showCompleted();

            Assert.AreEqual(ScreenState.Visible, ViewModel<AnimatedTestViewModel>().State);
            Assert.AreEqual(ScreenState.Focused, ViewModel<FirstTestViewModel>().State);
        }

        [Test]
        public void Elements_GetViewModelsFromThePool_AndFollowTheScreen()
        {
            AttachToPanel();

            _navigation.Push(TestScreen.First);

            CollectionAssert.AreEqual(
                new[] { "First.Create", "Counter.Bind", "First.Show", "Counter.Show", "First.Focus", "Counter.Focus" }, _log.Take());

            var counter = View<FirstTestScreen>().Q<CounterElement>("counter");
            _navigation.Update();
            Assert.AreEqual(1, counter.BoundViewModel.Updates);

            var added = new CounterElement();
            View<FirstTestScreen>().List.Add(added);

            CollectionAssert.AreEqual(new[] { "Counter.Bind", "Counter.Show", "Counter.Focus" }, _log.Take(), "an added element catches up with the screen");

            var addedViewModel = added.BoundViewModel;
            added.RemoveFromHierarchy();

            CollectionAssert.AreEqual(new[] { "Counter.Blur", "Counter.Hide", "Counter.Unbind" }, _log.Take());
            Assert.IsNull(added.BoundViewModel);

            var another = new CounterElement();
            View<FirstTestScreen>().List.Add(another);

            Assert.AreSame(addedViewModel, another.BoundViewModel, "the view model came from the pool");
            Assert.AreEqual(2, _log.Created.OfType<CounterViewModel>().Count());
            _log.Take();

            _navigation.Push(TestScreen.Second);

            CollectionAssert.AreEqual(
                new[] { "Second.Create", "Counter.Blur", "Counter.Blur", "First.Blur", "Second.Show", "Second.Focus" }, _log.Take(),
                "losing the focus goes elements first");
        }

        [Test]
        public void ViewModelChanges_ReachTheBoundViews()
        {
            AttachToPanel();

            _navigation.Push(TestScreen.First);
            var screen = View<FirstTestScreen>();
            var element = screen.Q<CounterElement>("counter");
            var counter = element.BoundViewModel;

            ViewModel<FirstTestViewModel>().Touch("Title");
            counter.Touch("Amount");

            CollectionAssert.AreEqual(new[] { "Title" }, screen.Changes);
            CollectionAssert.AreEqual(new[] { "Amount" }, element.Changes);

            element.RemoveFromHierarchy();
            counter.Touch("Amount");

            CollectionAssert.AreEqual(new[] { "Amount" }, element.Changes, "an element without its view model hears nothing");
        }

        [Test]
        public void Dispose_HidesTheOpenScreens_AndDestroysAll()
        {
            AttachToPanel();

            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Second);
            _navigation.Pop();
            _navigation.Push(TestScreen.Dialog, "x");
            _log.Take();

            _navigation.Dispose();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Dialog.Blur", "Dialog.Hide", "Dialog.Destroy",
                    "Counter.Hide", "First.Hide", "Counter.Unbind", "First.Destroy",
                    "Second.Destroy",
                    "Counter.Destroy",
                },
                _log.Take());
            Assert.IsNull(_navigation.Root.parent);
            Assert.Throws<ObjectDisposedException>(() => _navigation.Push(TestScreen.First));
        }

        [Test]
        public void StaleShowAnimation_OfAReopenedScreen_DoesNotFocusIt()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Animated);
            var view = View<AnimatedTestScreen>();
            var staleShow = view.PendingShow;

            _navigation.Pop();
            view.PendingHide();
            _navigation.Push(TestScreen.Animated);
            staleShow();

            Assert.AreEqual(1, _log.Created.OfType<AnimatedTestViewModel>().Count(), "the same instance came back from the pool");
            Assert.AreEqual(ScreenState.Visible, ViewModel<AnimatedTestViewModel>().State, "the animation of the previous open does not count");

            view.PendingShow();

            Assert.AreEqual(ScreenState.Focused, ViewModel<AnimatedTestViewModel>().State);
        }

        [Test]
        public void Page_HidesTheScreensBelow_AndPopShowsThemAgain()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Second);
            _log.Take();

            _navigation.Push(TestScreen.Page);

            CollectionAssert.AreEqual(
                new[] { "Page.Create", "Second.Blur", "Page.Show", "Second.Hide", "First.Hide", "Page.Focus" }, _log.Take());
            Assert.AreEqual(ScreenState.Hidden, ViewModel<FirstTestViewModel>().State);
            Assert.AreEqual(DisplayStyle.None, View<FirstTestScreen>().style.display.value);
            Assert.IsTrue(_navigation.IsOpen(TestScreen.First), "a covered screen stays in the history");

            _navigation.Update();

            Assert.AreEqual(0, ViewModel<FirstTestViewModel>().Updates, "a covered screen gets no frames");
            Assert.AreEqual(1, ViewModel<PageTestViewModel>().Updates);

            _navigation.Pop();

            CollectionAssert.AreEqual(new[] { "Page.Blur", "Page.Hide", "First.Show", "Second.Show", "Second.Focus" }, _log.Take());
            Assert.Greater(
                _navigation.Root.IndexOf(View<SecondTestScreen>()), _navigation.Root.IndexOf(View<FirstTestScreen>()),
                "the uncovered screens keep their order");
        }

        [Test]
        public void Page_HidesTheScreensBelow_OnlyAfterItsShowAnimation()
        {
            _navigation.Push(TestScreen.First);
            _log.Take();

            _navigation.Push(TestScreen.AnimatedPage);

            CollectionAssert.AreEqual(new[] { "Animated.Create", "First.Blur", "Animated.Show" }, _log.Take());
            Assert.AreEqual(ScreenState.Visible, ViewModel<FirstTestViewModel>().State, "visible under the page while it animates");

            View<AnimatedTestScreen>().PendingShow();

            CollectionAssert.AreEqual(new[] { "First.Hide", "Animated.Focus" }, _log.Take());
        }

        [Test]
        public void Page_ClosedWhileShowing_LeavesTheScreensBelowVisible()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.AnimatedPage);
            var view = View<AnimatedTestScreen>();
            var showCompleted = view.PendingShow;
            _log.Take();

            _navigation.Pop();
            showCompleted();
            view.PendingHide();

            CollectionAssert.AreEqual(new[] { "First.Focus", "Animated.Hide" }, _log.Take(), "the screen below was never hidden");
        }

        [Test]
        public void Replace_OfAPageWithAPage_KeepsTheScreensBelowHidden()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Page);
            _log.Take();

            _navigation.Replace(TestScreen.AnimatedPage);

            CollectionAssert.AreEqual(new[] { "Animated.Create", "Page.Blur", "Page.Hide", "Animated.Show" }, _log.Take());
            Assert.AreEqual(ScreenState.Hidden, ViewModel<FirstTestViewModel>().State, "not shown for the time of the animation");

            View<AnimatedTestScreen>().PendingShow();
            _navigation.Pop();

            CollectionAssert.AreEqual(new[] { "Animated.Focus", "Animated.Blur", "First.Show", "First.Focus" }, _log.Take());
        }

        [Test]
        public void Replace_OfAPageWithAnOverlay_ShowsTheScreensBelow()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Page);
            _log.Take();

            _navigation.Replace(TestScreen.Second);

            CollectionAssert.AreEqual(
                new[] { "Second.Create", "Page.Blur", "Page.Hide", "Second.Show", "First.Show", "Second.Focus" }, _log.Take());
            Assert.Greater(
                _navigation.Root.IndexOf(View<SecondTestScreen>()), _navigation.Root.IndexOf(View<FirstTestScreen>()),
                "the uncovered screen stays below the one opened after it");
        }

        [Test]
        public void PopTo_ThroughAPage_ShowsTheScreensItCovered()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Page);
            _navigation.Push(TestScreen.Second);
            _log.Take();

            _navigation.PopTo(TestScreen.First);

            CollectionAssert.AreEqual(new[] { "Second.Blur", "Second.Hide", "Page.Hide", "First.Show", "First.Focus" }, _log.Take());
        }

        [Test]
        public void CoveredScreen_ClosedFromTheMiddle_ReturnsToThePoolAtOnce()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Page);
            _log.Take();

            ViewModel<FirstTestViewModel>().CloseSelf();

            Assert.IsEmpty(_log.Take(), "hidden already, nothing to animate");
            Assert.IsFalse(_navigation.IsOpen(TestScreen.First));

            _navigation.Pop();
            _log.Take();
            _navigation.Push(TestScreen.First);

            CollectionAssert.AreEqual(new[] { "First.Show", "First.Focus" }, _log.Take(), "reused from the pool");
        }

        [Test]
        public void Dispose_DestroysCoveredScreens_WithoutHidingThemTwice()
        {
            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Page);
            _log.Take();

            _navigation.Dispose();

            CollectionAssert.AreEqual(new[] { "Page.Blur", "Page.Hide", "Page.Destroy", "First.Destroy" }, _log.Take());
        }

        [Test]
        public void Args_ThatThrow_ReturnTheScreenToThePool()
        {
            _navigation.Push(TestScreen.First);

            Assert.Throws<InvalidOperationException>(() => _navigation.Push(TestScreen.Dialog, DialogTestViewModel.THROWING_ARGS));
            Assert.IsFalse(_navigation.IsOpen(TestScreen.Dialog));

            _navigation.Push(TestScreen.Dialog, "ok");

            Assert.AreEqual(1, _log.Created.OfType<DialogTestViewModel>().Count(), "the instance went back to the pool");
            Assert.AreEqual(2, _navigation.Root.childCount, "no orphan screen left in the root");
        }

        [Test]
        public void ScreenThatFailsToCreate_DestroysItsViewModel_AndKeepsTheHistory()
        {
            _navigation.Push(TestScreen.First);
            _log.Take();

            var exception = Assert.Throws<InvalidOperationException>(() => _navigation.Push(TestScreen.Broken));

            Assert.AreEqual("The view failed to bind.", exception.Message, "the original exception comes out");
            CollectionAssert.AreEqual(new[] { "Broken.Create", "Broken.Destroy" }, _log.Take());
            Assert.AreEqual(1, _navigation.Root.childCount);
            Assert.AreEqual(ScreenState.Focused, ViewModel<FirstTestViewModel>().State);
        }

        [Test]
        public void CustomPools_HoldScreensAndElementViewModels_AndDropWhatTheyDoNotKeep()
        {
            var pools = new UnityObjectPoolFactory(maxSize: 1);
            _navigation.Dispose();
            _navigation = new Navigation<TestScreen>(_catalog, new TestViewModelFactory(_log), pools);
            AttachToPanel();

            _navigation.Push(TestScreen.First);
            _navigation.Push(TestScreen.Dialog, "a");
            _navigation.Push(TestScreen.Dialog, "b");
            _log.Take();

            _navigation.PopTo(TestScreen.First);

            CollectionAssert.AreEqual(
                new[] { "Dialog.Blur", "Dialog.Hide", "Dialog.Hide", "Dialog.Destroy", "First.Focus", "Counter.Focus" }, _log.Take(),
                "the pool keeps one sleeping dialog and drops the other");
            Assert.AreEqual(3, pools.Created, "pools of First, Dialog and the counter view model");

            _navigation.Push(TestScreen.Dialog, "c");

            Assert.AreEqual(2, _log.Created.OfType<DialogTestViewModel>().Count(), "the kept dialog is reused");
        }

        [Test]
        public void ActivatorFactory_CreatesViewModelsWithoutDependencies()
        {
            var viewModel = new ActivatorViewModelFactory().Create(typeof(PlainTestViewModel));

            Assert.IsInstanceOf<PlainTestViewModel>(viewModel);
        }

        [Test]
        public void ActivatorFactory_RejectsAViewModelWithDependencies()
        {
            Assert.Throws<MissingMethodException>(() => new ActivatorViewModelFactory().Create(typeof(FirstTestViewModel)));
        }

        private T ViewModel<T>() where T : ScreenViewModel
        {
            return _log.Created.OfType<T>().Single();
        }

        private T View<T>() where T : ScreenView
        {
            return _navigation.Root.Children().OfType<T>().Single();
        }

        /// <summary>Puts the navigation root into a UIDocument: the EditMode test runner keeps its own scene, the object lives until TearDown.</summary>
        private void AttachToPanel()
        {
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelHost = new GameObject(nameof(NavigationTests));
            _panelHost.SetActive(false);

            var document = _panelHost.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            _panelHost.SetActive(true);

            document.rootVisualElement.Add(_navigation.Root);
            Assert.IsNotNull(_navigation.Root.panel, "the navigation root is in a panel");
        }

        private static TestScreens CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<TestScreens>();
            var serialized = new SerializedObject(catalog);
            var screens = serialized.FindProperty("_screens");
            var entries = new[]
            {
                (TestScreen.First, "FirstTestScreen", ScreenMode.Overlay),
                (TestScreen.Second, "SecondTestScreen", ScreenMode.Overlay),
                (TestScreen.Dialog, "DialogTestScreen", ScreenMode.Overlay),
                (TestScreen.Animated, "AnimatedTestScreen", ScreenMode.Overlay),
                (TestScreen.Page, "PageTestScreen", ScreenMode.Page),
                (TestScreen.AnimatedPage, "AnimatedTestScreen", ScreenMode.Page),
                (TestScreen.Broken, "BrokenTestScreen", ScreenMode.Overlay),
            };

            screens.arraySize = entries.Length;

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = screens.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_screen").enumValueIndex = (int)entries[i].Item1;
                entry.FindPropertyRelative("_view").objectReferenceValue = LoadUxml(entries[i].Item2);
                entry.FindPropertyRelative("_mode").enumValueIndex = (int)entries[i].Item3;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            return catalog;
        }

        private static VisualTreeAsset LoadUxml(string name)
        {
            var path = AssetDatabase.FindAssets(name + " t:VisualTreeAsset")
                                    .Select(AssetDatabase.GUIDToAssetPath)
                                    .Single(assetPath => assetPath.EndsWith("/" + name + ".uxml"));

            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }
    }
}
