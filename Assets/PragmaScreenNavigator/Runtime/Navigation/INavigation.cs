using System;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// Navigation between UI Toolkit screens named by the enum <typeparamref name="TScreen"/>:
    /// <c>_navigation.Push(ScreenType.Home)</c>. Screens form a stack, the history: the top one is focused and receives
    /// input, the ones below stay visible but get no input. A closed screen goes back to the pool of its id and opens again
    /// without new UXML or a new view model, so the same screen can be in the history twice (two confirm dialogs).
    /// </summary>
    public interface INavigation<TScreen> where TScreen : struct, Enum
    {
        /// <summary>The top screen of the history; false when the history is empty.</summary>
        bool TryGetCurrent(out TScreen screen);

        /// <summary>Whether the screen is in the history.</summary>
        bool IsOpen(TScreen screen);

        /// <summary>Opens the screen over the current one, which stays visible but loses the focus.</summary>
        void Push(TScreen screen);

        /// <summary>Opens the screen over the current one and hands <paramref name="args"/> to its view model (<see cref="IScreenArgs{TArgs}"/>).</summary>
        void Push<TArgs>(TScreen screen, TArgs args);

        /// <summary>Replaces the current screen: the current one closes and the new one takes its place.</summary>
        void Replace(TScreen screen);

        /// <summary>Replaces the current screen and hands <paramref name="args"/> to the view model of the new one.</summary>
        void Replace<TArgs>(TScreen screen, TArgs args);

        /// <summary>Closes the current screen; the one below gets the focus.</summary>
        void Pop();

        /// <summary>Closes the screens above the topmost <paramref name="screen"/> of the history; does nothing when it is not open.</summary>
        void PopTo(TScreen screen);

        /// <summary>Closes every screen.</summary>
        void PopAll();

        /// <summary>
        /// Back (Esc): closes the focused screen if its view model agrees (<see cref="ScreenViewModel.OnBack"/>).
        /// Binding a key to it is up to the scene. Returns true when the screen is closed.
        /// </summary>
        bool Back();
    }
}
