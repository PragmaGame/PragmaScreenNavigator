namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The state of a screen in the navigation. A screen moves one step at a time: show (Hidden → Visible), focus
    /// (Visible → Focused), blur (Focused → Visible), hide (Visible → Hidden), and on every step its view, view model and
    /// elements get their call.
    /// </summary>
    public enum ScreenState
    {
        /// <summary>Not visible: not opened yet or sleeping in the pool.</summary>
        Hidden = 0,

        /// <summary>Visible but without input: under another screen, or in its show or hide animation.</summary>
        Visible = 1,

        /// <summary>The top screen: visible and receiving input.</summary>
        Focused = 2,
    }
}
