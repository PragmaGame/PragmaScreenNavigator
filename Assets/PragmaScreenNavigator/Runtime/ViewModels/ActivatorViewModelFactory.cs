using System;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// View models without DI: created through their public parameterless constructor. The constructor is reached only
    /// through reflection; the build step of the package keeps it from managed code stripping.
    /// </summary>
    public sealed class ActivatorViewModelFactory : IViewModelFactory
    {
        public ViewModel Create(Type type)
        {
            return (ViewModel)Activator.CreateInstance(type);
        }
    }
}
