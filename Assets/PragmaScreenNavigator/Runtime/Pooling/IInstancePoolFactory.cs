using System;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// Creates the pools of a navigation: one per screen id and one per element view model type. Implement it over the pool
    /// of your project; without one the navigation uses <see cref="StackInstancePoolFactory"/>.
    /// </summary>
    public interface IInstancePoolFactory
    {
        /// <param name="create">Creates a new instance when the pool has no sleeping one.</param>
        /// <param name="destroy">
        /// Destroys an instance the pool drops. Disposing the pool has to destroy every sleeping instance through it; instances
        /// that are out of the pool are the navigation's to destroy.
        /// </param>
        IInstancePool<T> Create<T>(Func<T> create, Action<T> destroy) where T : class;
    }
}
