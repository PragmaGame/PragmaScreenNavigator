using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>
    /// <see cref="DocumentNavigation{TScreen}"/>: the navigation of a document created from code — its root in the document,
    /// again after the document builds a new root, the frame and disposal.
    /// </summary>
    public class DocumentNavigationTests
    {
        private TestLog _log;
        private TestScreens _catalog;
        private PanelSettings _panelSettings;
        private GameObject _host;
        private UIDocument _document;
        private DocumentNavigation<TestScreen> _navigation;

        [SetUp]
        public void SetUp()
        {
            _log = new TestLog();
            _catalog = NavigationTests.CreateCatalog();
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

            // Inactive while it is put together: a document must not enable without its panel.
            _host = new GameObject(nameof(DocumentNavigationTests));
            _host.SetActive(false);
            _document = _host.AddComponent<UIDocument>();
            _document.panelSettings = _panelSettings;
        }

        [TearDown]
        public void TearDown()
        {
            _navigation?.Dispose();
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_panelSettings);
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void Attach_PutsTheRootIntoTheDocument_ThatLetsThePointerThrough()
        {
            _navigation = new DocumentNavigation<TestScreen>(_document, _catalog, new TestViewModelFactory(_log));
            Assert.IsFalse(_navigation.Attach(), "a disabled document has no root yet");

            _host.SetActive(true);

            Assert.IsTrue(_navigation.Attach());
            Assert.IsTrue(_navigation.IsAttached);
            Assert.AreSame(_document.rootVisualElement, _navigation.Navigation.Root.parent);
            Assert.AreEqual(PickingMode.Ignore, _document.rootVisualElement.pickingMode);

            _navigation.Attach();
            Assert.AreEqual(1, _document.rootVisualElement.childCount, "attached once");
        }

        [Test]
        public void Update_AttachesToTheNewRoot_OfADocumentEnabledAgain()
        {
            _navigation = new DocumentNavigation<TestScreen>(_document, _catalog, new TestViewModelFactory(_log));
            _host.SetActive(true);
            _navigation.Navigation.Push(TestScreen.First);
            _navigation.Update();

            _host.SetActive(false);
            _host.SetActive(true);
            Assert.IsFalse(_navigation.IsAttached, "the document built a new root");

            _navigation.Update();

            Assert.IsTrue(_navigation.IsAttached);
            Assert.IsTrue(_navigation.Navigation.IsOpen(TestScreen.First), "the screens stay open");
        }

        [Test]
        public void SafeAreaOff_LeavesTheRootOnTheWholeDocument()
        {
            _navigation = new DocumentNavigation<TestScreen>(_document, _catalog, new TestViewModelFactory(_log), isSafeAreaInset: false);
            _host.SetActive(true);
            _navigation.Update();

            var root = _navigation.Navigation.Root;
            Assert.AreEqual(0f, root.style.left.value.value);
            Assert.AreEqual(0f, root.style.top.value.value);
            Assert.AreEqual(0f, root.style.right.value.value);
            Assert.AreEqual(0f, root.style.bottom.value.value);
        }

        [Test]
        public void Dispose_DisposesTheNavigation_Once()
        {
            _navigation = new DocumentNavigation<TestScreen>(_document, _catalog, new TestViewModelFactory(_log));
            _host.SetActive(true);
            _navigation.Navigation.Push(TestScreen.First);
            _log.Take();

            _navigation.Dispose();
            _navigation.Dispose();

            var calls = _log.Take();
            Assert.AreEqual(1, calls.Count(call => call == "First.Hide"), "the open screen is hidden once");
            Assert.AreEqual(1, calls.Count(call => call == "First.Destroy"), "and destroyed once");
            Assert.Throws<ObjectDisposedException>(() => _navigation.Update());
            _navigation = null;
        }

        [Test]
        public void Constructor_WithoutADocument_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DocumentNavigation<TestScreen>(null, _catalog, new TestViewModelFactory(_log)));
        }
    }
}
