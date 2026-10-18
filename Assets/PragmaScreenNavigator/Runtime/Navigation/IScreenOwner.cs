namespace Pragma.ScreenNavigator
{
    /// <summary>The navigation that owns a screen: the view model closes its screen through it.</summary>
    internal interface IScreenOwner
    {
        void Close(ScreenViewModel viewModel);
    }
}
