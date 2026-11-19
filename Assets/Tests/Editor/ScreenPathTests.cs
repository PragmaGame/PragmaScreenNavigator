using System;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary><see cref="ScreenPath{TScreen}"/>: the screens it opens in order with their arguments, and that it never changes.</summary>
    public class ScreenPathTests
    {
        private TestLog _log;
        private TestScreens _catalog;
        private Navigation<TestScreen> _navigation;

        [SetUp]
        public void SetUp()
        {
            _log = new TestLog();
            _catalog = NavigationTests.CreateCatalog();
            _navigation = new Navigation<TestScreen>(_catalog, new TestViewModelFactory(_log));
        }

        [TearDown]
        public void TearDown()
        {
            _navigation.Dispose();
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void Open_PushesTheScreensInOrder_OverWhatIsOpen_WithTheirArgs()
        {
            _navigation.Push(TestScreen.First);
            _log.Take();

            ScreenPath<TestScreen>.To(TestScreen.Second).Then(TestScreen.Dialog, "hello").Open(_navigation);

            CollectionAssert.AreEqual(new[]
            {
                "Second.Create", "First.Blur", "Second.Show", "Second.Focus",
                "Dialog.Create", "Dialog.Args hello", "Second.Blur", "Dialog.Show", "Dialog.Focus",
            }, _log.Take());
            Assert.IsTrue(_navigation.IsOpen(TestScreen.First), "the path goes over what was open");

            _navigation.Back();
            Assert.IsTrue(_navigation.TryGetCurrent(out var current));
            Assert.AreEqual(TestScreen.Second, current, "Back walks the path back");
        }

        [Test]
        public void To_WithArgs_OpensTheFirstScreenWithThem()
        {
            ScreenPath<TestScreen>.To(TestScreen.Dialog, "first").Open(_navigation);

            CollectionAssert.Contains(_log.Take(), "Dialog.Args first");
        }

        [Test]
        public void Then_ReturnsANewPath_AndLeavesThisOne()
        {
            var path = ScreenPath<TestScreen>.To(TestScreen.First);
            var longer = path.Then(TestScreen.Second);

            Assert.AreEqual(1, path.Length);
            Assert.AreEqual(2, longer.Length);

            path.Open(_navigation);
            Assert.IsFalse(_navigation.IsOpen(TestScreen.Second), "the kept path did not grow");
        }

        [Test]
        public void Open_WithoutANavigation_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ScreenPath<TestScreen>.To(TestScreen.First).Open(null));
        }
    }
}
