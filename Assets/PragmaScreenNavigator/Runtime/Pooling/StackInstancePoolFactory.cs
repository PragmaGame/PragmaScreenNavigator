using System;

namespace Pragma.ScreenNavigator
{
    /// <summary>The default pools of a navigation: unbounded <see cref="StackInstancePool{T}"/>.</summary>
    public sealed class StackInstancePoolFactory : IInstancePoolFactory
    {
        public IInstancePool<T> Create<T>(Func<T> create, Action<T> destroy) where T : class
        {
            return new StackInstancePool<T>(create, destroy);
        }
    }
}
