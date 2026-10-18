namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The view model of a screen that is opened with data: <c>Push(screen, args)</c> and <c>Replace(screen, args)</c> hand
    /// it over before the screen is shown. The screen instance comes from a pool, so the data is new every time.
    /// </summary>
    public interface IScreenArgs<in TArgs>
    {
        void SetArgs(TArgs args);
    }
}
