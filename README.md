  # PragmaScreenNavigator

Screen navigation for UI Toolkit. Screens are named by an enum, built from UXML and driven by MVVM view models bound
through runtime data binding.

## Features

- **History stack** — `Push`, `Replace`, `Pop`, `PopTo`, `PopAll`; the top screen is focused
- **Pages and overlays** — an overlay keeps the screens below visible, a page hides what it covers, so nothing renders
  or updates behind a full-screen menu
- **Input blocking** — a screen out of focus gets no pointer, keyboard or navigation events, its focus and pointer
  captures are released
- **Pooled screens** — a closed screen sleeps hidden in the root and reopens as the same instance, without cloning UXML
  or creating a view model; the same screen can be in the history twice
- **Pluggable pools** — screens and element view models live in pools from an `IInstancePoolFactory`: the built-in one,
  or an adapter over the pool of your project
- **MVVM** — `ViewModel` implements `INotifyBindablePropertyChanged`, UXML bindings read its `[CreateProperty]` properties
- **Composite screens** — `ScreenElement<TViewModel>` parts with their own pooled view models that follow the screen
  lifecycle, placed in UXML or added at run time
- **Open arguments** — `Push(screen, args)` hands data to the view model before the screen is shown
- **Back** — `Back()` closes the focused screen unless its view model refuses
- **Animations** — `AnimateShow` / `AnimateHide` hooks; focus and the return to the pool wait for them
- **Lists** — `SelectableList`, a list of rows with a selection over its own view model
- **Input to the game** — whether the pointer is over the UI and whether the player types into a field
- **DI-agnostic** — view models come from an `IViewModelFactory` you implement over any container
- **Stripping-safe** — a build step keeps the constructors of your view models under IL2CPP managed code stripping

## Installation

Install via git URL:

```
https://github.com/PragmaGame/PragmaScreenNavigator.git?path=Assets/PragmaScreenNavigator
```

The package has no dependencies outside Unity.

## Quick start

### 1. Name the screens and list their UXML

```csharp
public enum ScreenType
{
    Home = 0,
    Settings = 1,
    Confirm = 2,
}

[CreateAssetMenu(menuName = "UI/" + nameof(MenuScreens))]
public sealed class MenuScreens : ScreenCatalog<ScreenType>
{
}
```

Create the asset and give every screen a row with its UXML and its mode: `Page` for opaque full-screen screens, `Overlay`
for dialogs and popups (see [Pages and overlays](#pages-and-overlays)).

### 2. A view model

```csharp
[GeneratePropertyBag]
public class HomeViewModel : ScreenViewModel
{
    private readonly INavigation<ScreenType> _navigation;
    private string _title;

    public HomeViewModel(INavigation<ScreenType> navigation)
    {
        _navigation = navigation;
    }

    [CreateProperty]
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public void OpenSettings()
    {
        _navigation.Push(ScreenType.Settings);
    }

    protected override bool OnBack()
    {
        return false;   // nothing below the home screen
    }
}
```

`[GeneratePropertyBag]` makes the property bag generated code instead of reflection, which IL2CPP strips.

### 3. A view — the root of the UXML

```csharp
[UxmlElement]
public partial class HomeScreen : ScreenView<HomeViewModel>
{
    private Button _settings;

    protected override void OnCreate()
    {
        _settings = this.Q<Button>("settings");
    }

    protected override void OnBind(HomeViewModel viewModel)
    {
        _settings.clicked += viewModel.OpenSettings;
    }

    protected override void OnUnbind(HomeViewModel viewModel)
    {
        _settings.clicked -= viewModel.OpenSettings;
    }
}
```

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:menu="MyGame.Menu">
    <menu:HomeScreen>
        <ui:Label>
            <Bindings>
                <ui:DataBinding property="text" data-source-path="Title" binding-mode="ToTarget" />
            </Bindings>
        </ui:Label>
        <ui:Button name="settings" text="Settings" />
    </menu:HomeScreen>
</ui:UXML>
```

The view model is the `dataSource` of the screen, so bindings inside it resolve against the view model. What bindings do
not cover (visibility, classes, list rows) goes to `OnViewModelChanged(property)`.

### 4. Host the navigation in a scene

`NavigationHost<TScreen, TCatalog>` adds the navigation root to a `UIDocument`, opens the start screen, passes the frame
to the screens and disposes the navigation with the scene. It knows no DI container: the subclass creates the navigation
with the view model factory it wants.

Without DI — view models need a public parameterless constructor:

```csharp
public sealed class MenuNavigation : NavigationHost<ScreenType, MenuScreens>
{
    private void Awake()
    {
        CreateNavigation(new ActivatorViewModelFactory());
    }
}
```

With a DI container — implement `IViewModelFactory` over it and register the navigation, so view models can take
`INavigation<ScreenType>` and any other service. Reflex, for example:

```csharp
public sealed class ReflexViewModelFactory : IViewModelFactory
{
    private readonly Container _container;

    public ReflexViewModelFactory(Container container)
    {
        _container = container;
    }

    public ViewModel Create(Type type)
    {
        return (ViewModel)_container.Construct(type);
    }
}

public sealed class MenuNavigation : NavigationHost<ScreenType, MenuScreens>, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.RegisterFactory(
            container => CreateNavigation(new ReflexViewModelFactory(container)),
            new[] { typeof(INavigation<ScreenType>) },
            Lifetime.Singleton,
            Resolution.Eager);
    }
}
```

The navigation has to exist by `Start`, so register it eagerly. The host disposes it in `OnDestroy`; `Dispose` is
idempotent, so a container that disposes it too does no harm.

Back is up to the scene — bind a key to it in the host:

```csharp
protected override void Update()
{
    if (Keyboard.current.escapeKey.wasPressedThisFrame)
    {
        Navigation.Back();
    }

    base.Update();
}
```

`Navigation<TScreen>` works without the host as well: create it, add `navigation.Root` to a document, call
`navigation.Update()` every frame and `Dispose()` at the end. Both take an optional `IInstancePoolFactory` after the
view model factory (see [Pools](#pools)).

## Lifecycle

```
Hidden ──show──▶ Visible ──focus──▶ Focused
       ◀──hide──         ◀──blur───
```

| Call | Order | When |
|---|---|---|
| `OnCreate` | view, then view model | once per screen instance |
| `OnShow`, `OnFocus` | view → view model → elements | opened; became the top screen |
| `OnBlur`, `OnHide` | elements → view model → view | another screen on top; closed |
| `OnUpdate` | view model → elements | every `Update` while visible |
| `OnDestroy` | view model | its pool drops the instance, or the navigation is disposed |

A screen instance is reused, so whatever lives from show to hide — subscriptions, timers — starts in `OnShow` and ends
in `OnHide`. Every show is paired with a hide, including on dispose.

`Push` blurs the current screen and shows the new one. `Pop` hides the top screen and focuses the one below.

## Pages and overlays

Every row of the catalog has a `ScreenMode`:

- **Overlay** (the default) — the screens below stay visible under it and keep receiving `OnUpdate`, with their input
  blocked. Dialogs, popups, a menu over the HUD.
- **Page** — covers the screens below. Once its show animation ends they get `OnHide` and leave the layout, so nothing
  renders or updates behind it; they stay in the history. When the page leaves the history they get `OnShow` again and
  reappear in their places, under the screens opened after them.

```
Push(Home)       Home                       Focused
Push(Settings)   Home ▸ Settings(page)      Home: Blur, then Hide when the page has shown
Push(Confirm)    … ▸ Settings ▸ Confirm      Settings stays visible under the overlay
Pop, Pop         Home                       Home: Show, Focus
```

The screens below a page stay visible for the time of its show animation, so it can slide or fade in over them.
Replacing a page with another page keeps them hidden throughout. A covered screen closed from the middle of the history
returns to its pool at once, without a hide animation.

## Open arguments

```csharp
public sealed class ConfirmArgs
{
    public string Message;
    public Action Confirmed;
}

[GeneratePropertyBag]
public class ConfirmViewModel : ScreenViewModel, IScreenArgs<ConfirmArgs>
{
    public void SetArgs(ConfirmArgs args) { ... }   // before OnShow, on every open
}

_navigation.Push(ScreenType.Confirm, new ConfirmArgs { Message = "Quit?", Confirmed = Quit });
```

Pushing arguments to a screen whose view model does not implement `IScreenArgs<TArgs>` throws and leaves the history
untouched; so does a `SetArgs` that throws, and the instance goes back to its pool either way. A screen that fails to
create (its view or view model throws in `OnCreate` or `OnBind`) is cleaned up — a view model that has run `OnCreate`
gets `OnDestroy` — and the original exception comes out.

A view model closes its own screen with `Close()` — the top one like `Pop`, one in the middle of the history without
touching the screens above it.

## Animations

```csharp
protected override void AnimateShow(Action completed)
{
    // the screen is visible; it gets the focus when completed() is called
}

protected override void AnimateHide(Action completed)
{
    // the screen has lost the focus; it hides and returns to the pool on completed()
    // stop a show animation that may still be running
}
```

A screen closed in the middle of its show animation is not focused when that animation ends, and a show animation of an
earlier open never focuses a screen that has been reopened since. A screen pushed again while its previous instance is
still hiding gets a new instance. A screen uncovered by a closing page plays its show animation again.

## Composite screens

A screen part with its own view model — a resource bar, a minimap, a list row:

```csharp
public class ResourceBarViewModel : ElementViewModel
{
    protected override void OnShow() { /* subscribe */ }
    protected override void OnHide() { /* unsubscribe */ }
    protected override void OnUnbind() { /* reset: the view model goes back to the pool */ }
    protected override void OnDestroy() { /* the pool dropped it, or the navigation is disposed */ }
}

[UxmlElement]
public partial class ResourceBar : ScreenElement<ResourceBarViewModel>
{
}
```

Put it into the screen UXML or add it at run time. Attached to a panel, the element registers on the nearest screen
above it and takes a view model of its type from the pool; detached, it returns it. An element added to a visible screen
catches up with it right away (`OnShow`, then `OnFocus`). Element view models come from the same `IViewModelFactory`.

Elements register only while the screen is in a panel, so add `navigation.Root` to a document before relying on them.

## Lists

A list of rows with a selection — saved games, open lobbies, languages, the players of a lobby — is a
`SelectableList` element over a `SelectableListViewModel`. The list view model is a part of the screen without a
lifecycle of its own: the screen's view model owns it and fills it, the screen view binds it.

```csharp
public class ReplaysViewModel : ScreenViewModel
{
    public SelectableListViewModel Replays { get; } = new SelectableListViewModel();

    protected override void OnShow()
    {
        Replays.Chosen += OnChosen;                 // the player's choice; -1 for none
        Replays.SetRows(LoadNames(), selected: 0);  // texts in order and the selected row
    }

    protected override void OnHide()
    {
        Replays.Chosen -= OnChosen;
    }
}

[UxmlElement]
public partial class ReplaysScreen : ScreenView<ReplaysViewModel>
{
    private SelectableList _replays;

    protected override void OnBind(ReplaysViewModel viewModel)
    {
        _replays = this.Q<SelectableList>("replays");
        _replays.Bind(viewModel.Replays);
    }

    protected override void OnUnbind(ReplaysViewModel viewModel)
    {
        _replays.Unbind();
    }
}
```

```xml
<Pragma.ScreenNavigator.SelectableList name="replays" row-class="replay-row" is-selectable="true" />
```

- Rows are `ListRowViewModel`s (`Text`, `IsSelected`), reused when the list is filled again: a list that refreshes
  itself does not create view models over and over. `Rows` notifies when their number changes, `Selected` when the
  selection does; a row notifies its own text and mark.
- Selectable rows are buttons that select on click and raise `Chosen`; with `is-selectable="false"` they are labels
  whose mark only shows (the player's own row in a lobby). A selection made by `SetRows` is not reported: the screen
  made it.
- The rows get the `row-class` (`selectable-list__row` by default), the selected one also `row-class--selected`; how
  they look is the screen's style.

## Pools

The navigation keeps one pool per screen id and one per element view model type. It asks an `IInstancePoolFactory` for
them, handing over two callbacks: `create` builds a new instance, `destroy` tears one down. A pool keeps whatever it
wants and drops the rest through `destroy`; disposing it destroys every sleeping instance.

Without a factory the navigation uses `StackInstancePoolFactory`: unbounded pools that keep everything until the
navigation is disposed. To plug in the pool of your project, write an adapter.

Over `UnityEngine.Pool.ObjectPool`, keeping at most `maxSize` sleeping instances per pool:

```csharp
public sealed class UnityObjectPoolFactory : IInstancePoolFactory
{
    private readonly int _maxSize;

    public UnityObjectPoolFactory(int maxSize)
    {
        _maxSize = maxSize;
    }

    public IInstancePool<T> Create<T>(Func<T> create, Action<T> destroy) where T : class
    {
        return new UnityObjectPool<T>(new ObjectPool<T>(create, actionOnDestroy: destroy, collectionCheck: false, maxSize: _maxSize));
    }

    private sealed class UnityObjectPool<T> : IInstancePool<T> where T : class
    {
        private readonly ObjectPool<T> _pool;

        public UnityObjectPool(ObjectPool<T> pool)
        {
            _pool = pool;
        }

        public T Get()
        {
            return _pool.Get();
        }

        public void Release(T instance)
        {
            _pool.Release(instance);
        }

        public void Dispose()
        {
            _pool.Dispose();
        }
    }
}
```

Over [Pragma Pool](https://github.com/PragmaGame/PragmaPool):

```csharp
public sealed class PragmaPoolFactory : IInstancePoolFactory
{
    public IInstancePool<T> Create<T>(Func<T> create, Action<T> destroy) where T : class
    {
        return new PragmaInstancePool<T>(create, destroy);
    }

    private sealed class PragmaInstancePool<T> : IInstancePool<T> where T : class
    {
        private readonly Pool<T> _pool;

        public PragmaInstancePool(Func<T> create, Action<T> destroy)
        {
            _pool = new Pool<T>(CreateFactory.Instance, create);
            _pool.Register(PoolSignal.Destroy, destroy);
        }

        public T Get()
        {
            return _pool.Spawn();
        }

        public void Release(T instance)
        {
            _pool.Release(instance);
        }

        public void Dispose()
        {
            _pool.Dispose();
        }
    }

    // Pragma Pool hands its create data to the factory: here it is the create callback itself.
    private sealed class CreateFactory : IPoolObjectFactory
    {
        public static readonly CreateFactory Instance = new CreateFactory();

        public T Create<T>(object data = null) where T : class
        {
            return ((Func<T>)data)();
        }
    }
}
```

Then pass it next to the view model factory: `CreateNavigation(new ActivatorViewModelFactory(), new PragmaPoolFactory())`
in a host, or `new Navigation<ScreenType>(catalog, viewModelFactory, new PragmaPoolFactory())`.

## Input

Screens and the navigation root have `pickingMode = Ignore`: empty space lets the pointer through to whatever is below
(the game world under a HUD), and only panels and buttons catch it. A screen out of focus stops pointer, keyboard and
navigation events at its root, so a click on a visible screen below a dialog reaches neither its buttons nor the world.
A modal dialog keeps clicks off everything below with a full-size backdrop element inside the screen: child elements
catch the pointer by default.

The game asks the panel before it treats a click or a key as its own (`PanelExtensions`, on the `panel` of the
document's root):

```csharp
var panel = document.rootVisualElement.panel;
if (panel.IsPointerOverUi(pointerPosition))   // pixels from the bottom left, as the Input System gives them
{
    return;                                   // the click belongs to the UI, not to the units under it
}
if (panel.IsTyping())                         // the focus is in a text field: hotkeys stay silent
{
    return;
}
```

Give the document's root `pickingMode = Ignore` too, or every point of the screen counts as UI. `IsTyping` holds for
every field the player types into (`TextField`, `IntegerField`...); `element.IsTextInput()` asks the same of one
element. Both answer false for a panel that is not there yet.

## Code stripping

View models are created through reflection — `Activator.CreateInstance` or a DI container — so their constructors look
unused to the linker, and Medium or High managed code stripping would remove them. The package ships a build step
(`ScreenNavigatorLinkerProcessor`) that generates a `link.xml` for every player build, keeping every concrete view
model of the assemblies that reach the player whole. Nothing to mark by hand.

Property bags are generated with `[GeneratePropertyBag]`. Views and elements are referenced by the UXML serialization and
need nothing.

## Requirements

- Unity 6000.0+ (runtime data binding and `[UxmlElement]`)
