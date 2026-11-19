# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `SelectableList`, `SelectableListViewModel`, `ListRowViewModel`: a list of rows with a selection. The screen's view
  model owns the list view model and fills it with `SetRows(texts, selected)`, the player's pick arrives through
  `Chosen`; rows are reused, `Rows` notifies a new count and `Selected` a new selection. The element shows buttons
  (`is-selectable`) or labels, with the `row-class` of the UXML and its `--selected` modifier.
- `PanelExtensions.IsPointerOverUi(panel, screenPosition)` and `PanelExtensions.IsTyping(panel)`: whether a click
  belongs to the UI rather than the world under it, and whether the focus is in a text field, so the game's hotkeys stay
  silent; `VisualElementExtensions.IsTextInput(element)` for one element.
- `SelectableItems<TItem>`: the items behind the rows of a `SelectableList`, with their texts and the selection kept on
  an item by its key when the list is refilled; `SelectedItem`, `SetItems(items, isFirstSelected)`, `Select(index)`.
- `ScreenPath<TScreen>`: an immutable path of screens with their open arguments (`To`, `Then`), opened later in order
  over what is open (`Open(navigation)`), so Back walks the way the player came.
- `DocumentNavigation<TScreen>`: the navigation of a `UIDocument` created from code — owns the navigation, puts its root
  into the document whenever the document builds a new one, keeps the safe area and passes the frame.
- `SafeAreaInset`: keeps an absolutely positioned element inside `Screen.safeArea` (a camera cutout, rounded corners,
  the system bars) in the units of its panel; `GetInsets` for the margins alone.

### Changed

- `NavigationHost` keeps the screens inside the screen's safe area (*Is Safe Area Inset*, on by default) and does its
  document part through `DocumentNavigation`.

## [0.1.0]

### Added

- Initial release, extracted from a game project.
- `INavigation<TScreen>` / `Navigation<TScreen>`: a history of UI Toolkit screens named by an enum, with focus, input
  blocking, pooled screen instances, open arguments, Back and show/hide animations.
- `ScreenMode`: an overlay keeps the screens below visible, a page hides the screens it covers once its show animation
  ends and shows them again when it leaves the history.
- `ScreenCatalog<TScreen>`: the UXML and the mode of each screen.
- `ScreenView<TViewModel>`, `ScreenViewModel`, `ViewModel`: MVVM over runtime data binding.
- `ScreenElement<TViewModel>`, `ElementViewModel`: composite screens made of elements with pooled view models.
- `IViewModelFactory` and `ActivatorViewModelFactory`: view models from any DI container, or without one.
- `IInstancePoolFactory` / `IInstancePool<T>` and the default `StackInstancePoolFactory`: screens and element view
  models live in pools of your choice.
- `NavigationHost<TScreen, TCatalog>`: hosts the navigation of a scene on a `UIDocument` without knowing the DI container.
- `ScreenNavigatorLinkerProcessor`: keeps the constructors of concrete view models from managed code stripping in
  player builds.

### Changed (compared to the game code it came from)

- No dependency on a DI container: the Reflex installer became the container-agnostic `NavigationHost`.
- No dependency on a pool package.
- A `SetArgs` that throws returns the screen instance to its pool instead of leaving it orphaned in the root.
- A screen that fails to create destroys its view model, and its original exception propagates.
- A show animation that ends after its screen was closed, covered or reopened no longer focuses it.
- `ElementViewModel.OnDestroy`: element view models are told when their pool drops them.
