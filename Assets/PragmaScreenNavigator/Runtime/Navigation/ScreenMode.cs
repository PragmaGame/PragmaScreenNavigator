namespace Pragma.ScreenNavigator
{
    /// <summary>How a screen treats the screens below it in the history.</summary>
    public enum ScreenMode
    {
        /// <summary>The screens below stay visible and keep getting frames: dialogs, popups, a menu over the HUD.</summary>
        Overlay = 0,

        /// <summary>
        /// Covers the screens below: once its show animation ends they hide and get no frames, and they show again when it
        /// leaves the history. For opaque full-screen screens, so nothing renders or updates behind them.
        /// </summary>
        Page = 1,
    }
}
