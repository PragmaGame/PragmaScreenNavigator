using System;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// Creates screen and element view models by type, so the navigation does not need to know where their dependencies
    /// come from. A DI integration implements it over its container; without DI there is <see cref="ActivatorViewModelFactory"/>.
    /// </summary>
    public interface IViewModelFactory
    {
        ViewModel Create(Type type);
    }
}
