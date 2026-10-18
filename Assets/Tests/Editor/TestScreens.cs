namespace Pragma.ScreenNavigator.Tests
{
    public enum TestScreen
    {
        First = 0,
        Second = 1,
        Dialog = 2,
        Animated = 3,
        Missing = 4,
        Page = 5,
        AnimatedPage = 6,
        Broken = 7,
    }

    /// <summary>
    /// The catalog of the test screens: the test fills its rows (Screens/*.uxml). Missing has no row, Page and AnimatedPage
    /// are pages, AnimatedPage shares its UXML with Animated.
    /// </summary>
    public sealed class TestScreens : ScreenCatalog<TestScreen>
    {
    }
}
