using System;
using UnityEngine.Pool;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>
    /// Pools over UnityEngine.Pool.ObjectPool, plugged in the way a project plugs its own pool in: each keeps at most
    /// maxSize sleeping instances and destroys the ones released above that.
    /// </summary>
    public sealed class UnityObjectPoolFactory : IInstancePoolFactory
    {
        private readonly int _maxSize;

        public UnityObjectPoolFactory(int maxSize)
        {
            _maxSize = maxSize;
        }

        public int Created { get; private set; }

        public IInstancePool<T> Create<T>(Func<T> create, Action<T> destroy) where T : class
        {
            Created++;

            return new UnityObjectPool<T>(new ObjectPool<T>(create, actionOnDestroy: destroy, collectionCheck: false, maxSize: _maxSize));
        }

        private sealed class UnityObjectPool<T> : IInstancePool<T> where T : class
        {
            private readonly ObjectPool<T> _pool;

            public UnityObjectPool(ObjectPool<T> pool)
            {
                _pool = pool;
            }

            public T Get()
            {
                return _pool.Get();
            }

            public void Release(T instance)
            {
                _pool.Release(instance);
            }

            public void Dispose()
            {
                _pool.Dispose();
            }
        }
    }
}
