# PragmaScreenNavigator — agent guide

The development project of the Unity package `com.pragma.screennavigator` — screen navigation for UI Toolkit. The
published package root is `Assets/PragmaScreenNavigator` (installed via
`https://github.com/PragmaGame/PragmaScreenNavigator.git?path=Assets/PragmaScreenNavigator`). Everything outside that
folder — `Assets/Tests`, `ProjectSettings` and the rest — is the development project and does **not** ship.
Editor version: see `ProjectSettings/ProjectVersion.txt`.

The package is pre-1.0: backward compatibility is not required yet, prefer clean renames over compatibility shims.

## Layout

| Path | Assembly | Ships |
|---|---|---|
| `Assets/PragmaScreenNavigator/Runtime/Navigation` | `Pragma.ScreenNavigator` | yes — `Navigation<TScreen>`, catalog, modes, host, states |
| `Assets/PragmaScreenNavigator/Runtime/Views` | `Pragma.ScreenNavigator` | yes — `ScreenView`, `ScreenElement`, input blocking, what the panel takes from the game's input (`PanelExtensions`) |
| `Assets/PragmaScreenNavigator/Runtime/ViewModels` | `Pragma.ScreenNavigator` | yes — view models, factories, element view model pool |
| `Assets/PragmaScreenNavigator/Runtime/Lists` | `Pragma.ScreenNavigator` | yes — `SelectableList` and its view models (`SelectableListViewModel`, `ListRowViewModel`) |
| `Assets/PragmaScreenNavigator/Runtime/Pooling` | `Pragma.ScreenNavigator` | yes — `IInstancePool<T>`, `IInstancePoolFactory`, the default stack pools |
| `Assets/PragmaScreenNavigator/Editor` | `Pragma.ScreenNavigator.Editor` | yes — `ScreenNavigatorLinkerProcessor` (generated link.xml) |
| `Assets/Tests/Editor` | `Pragma.ScreenNavigator.Tests.Editor` | no — EditMode tests and their UXML screens |

One namespace, `Pragma.ScreenNavigator`, for every folder. The package depends on nothing outside Unity.

## Architecture invariants

- **No third-party dependency in the package.** View models come from `IViewModelFactory`, pools from
  `IInstancePoolFactory`; container- and pool-specific adapters live in the consumer project (the README shows Reflex,
  `UnityEngine.Pool.ObjectPool` and Pragma Pool). `NavigationHost` only receives factories through `CreateNavigation`.
  Do not add a reference to a DI container or a pool package.
- **Pools own only sleeping instances.** `IInstancePool.Dispose` destroys what sleeps in it through the `destroy`
  callback; instances out of the pool are destroyed by the navigation. Pooled instances must not implement
  `IDisposable` (a third-party pool may dispose them on its own and they would be destroyed twice) — element view
  models get `OnDestroy` from the callback instead.
- **Visibility follows the modes.** The top screen is `Focused`; screens down to the topmost `ScreenMode.Page` are
  `Visible` with input blocked and still get `OnUpdate`; screens below a page that has finished showing are covered:
  `Hidden`, in the history, flagged `isCovered`. `UpdateCover` hides under a page that *has shown* but uncovers only
  what no page *in the history* is above (shown or still showing), so replacing a page with a page does not flicker.
  Uncovered screens show in place (no `BringToFront`), keeping the order of the history.
- **Late animation callbacks are ignored.** Every show, cover and close bumps `Screen.transition`; a show completion
  carries the value it started with. Do not check "is it still on top" instead — a pooled instance can be on top again.
- **Lifecycle order is fixed:** show and focus go view → view model → elements; blur and hide go elements → view model →
  view. States move one step at a time (`Hidden ↔ Visible ↔ Focused`), and every show is paired with a hide, including on
  `Dispose`. The tests pin the exact call sequences.
- **Screen instances are pooled by id, element view models by type.** A screen reopens as the same view and view model,
  so per-open state belongs between `OnShow` and `OnHide`, and open data arrives through `IScreenArgs<TArgs>` before
  `OnShow`. A screen that is still playing its hide animation is not in the pool, so pushing it again creates a new
  instance.
- **Re-entrancy:** a view model may open or close screens from any callback. Passes over the history and over elements
  go through snapshots and skip removed entries; `UpdateCover` defers a nested call and reruns. Keep it that way when
  adding passes.
- **Failures leave nothing behind.** A screen rented for `Push`/`Replace` goes back to its pool when `SetArgs` throws;
  a screen that fails in `CreateScreen` is destroyed (its view model gets `OnDestroy` only if its `OnCreate` finished),
  and the cleanup never hides the original exception.
- **Stripping.** View models are created through reflection, so `ScreenNavigatorLinkerProcessor` writes a link.xml
  keeping every concrete `ViewModel` of the player assemblies. A new reflection-only entry point needs the same.
- **Elements register only while their screen is in a panel** (`AttachToPanelEvent`). A screen built outside a panel
  (UI Builder, tests without a `UIDocument`) has elements without view models — that is intended.
- **Input blocking is event based** (`ScreenInputBlocker`): trickle-down callbacks on the screen root stop pointer,
  keyboard and navigation events; `SetEnabled(false)` is not used because it restyles the screen.

## Code conventions

- C#: 4 spaces, braces on their own lines and always present, block-bodied methods, `var` for locals, `_camelCase`
  private fields, `PascalCase` members, **UPPER_SNAKE_CASE constants** (`ScreenView.USS_CLASS_NAME`).
- **English everywhere in code**: comments, XML docs, `[Tooltip]`, log and exception messages.
- Comments explain *why* and only where the behaviour is not obvious from the call site; never narrate the code.
  XML `<summary>` on public API is documentation and is welcome — keep it terse.
- Boolean members and parameters are prefixed `is` (`IsBlocked`, `isDisplayed`).
- Concrete view models carry `[GeneratePropertyBag]`; views and elements are `[UxmlElement] partial` classes.
- Keep `README.md` and `CHANGELOG.md` in sync with public API changes.

## Verifying a change

The project usually has a live editor running with `com.unity.pipeline`, driven by the `unity` CLI
(`%LOCALAPPDATA%\Unity\bin\unity`), auto-discovered from the project directory:

```bash
unity --no-banner --json command editor_status                       # must report "ready"
unity --no-banner --json command menu --path "Assets/Refresh"        # import files changed outside the editor
unity --no-banner --json command console                             # compile errors and logs (old entries stay)
unity --no-banner --json command --timeout 600 run_tests --mode editor --filter_type assembly --filter Pragma.ScreenNavigator.Tests.Editor
```

The console keeps entries from earlier compilations: compare their timestamps with
`Library/ScriptAssemblies/Pragma.ScreenNavigator*.dll`.

## Tests

`Assets/Tests/Editor` is the EditMode suite. View models get a `TestLog` through `TestViewModelFactory` and log every
lifecycle call as `Name.Call`; tests assert exact sequences with `TestLog.Take()`. Tests that need elements put the
navigation root into a `UIDocument` (`AttachToPanel`). `AnimatedTestScreen` holds its animation completions for the test
to call, `UnityObjectPoolFactory` is a third-party pool adapter (also shown in the README). Name tests
`Subject_ExpectedBehaviour`.

The package must keep compiling on its minimum Unity version (`unity` in `package.json`). When an older editor is
installed, compile `Runtime` and `Editor` with its `DotNetSdkRoslyn/csc.dll` against its `Managed/UnityEngine` modules.

## Gotchas

- Every asset needs a `.meta` file. Let Unity generate it by refreshing rather than inventing a GUID by hand, and move
  files together with their `.meta`.
- The UXML root of a screen is cloned into a container that is thrown away: style sheets attached to the UXML root are
  moved onto the screen view in `Navigation.Instantiate`.
- `Navigation.Dispose` is idempotent on purpose: both `NavigationHost` and a DI container may dispose it.
- Do not `git commit` or `git push` without being asked.

## Git

Commit subjects (the first line of the message) are at most 72 characters: what was done, briefly. Details go to the
body, after a blank line. The maintainer prefers answers in Russian; everything in the repository stays in English.
