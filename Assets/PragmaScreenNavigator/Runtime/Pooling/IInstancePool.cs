using System;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// A pool the navigation reuses instances from: screen instances of one screen id, or element view models of one type.
    /// Created by <see cref="IInstancePoolFactory"/>, which hands it the create and destroy callbacks.
    /// </summary>
    public interface IInstancePool<T> : IDisposable where T : class
    {
        /// <summary>A sleeping instance, or a new one from the create callback.</summary>
        T Get();

        /// <summary>
        /// Takes an instance back. The navigation never releases an instance twice; the pool may keep it or drop it through
        /// the destroy callback (a size limit of its own).
        /// </summary>
        void Release(T instance);
    }
}
