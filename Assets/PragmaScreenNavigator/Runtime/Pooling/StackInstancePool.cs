using System;
using System.Collections.Generic;

namespace Pragma.ScreenNavigator
{
    /// <summary>An unbounded pool over a stack: keeps every released instance until it is disposed.</summary>
    public sealed class StackInstancePool<T> : IInstancePool<T> where T : class
    {
        private readonly Stack<T> _sleeping = new Stack<T>();
        private readonly Func<T> _create;
        private readonly Action<T> _destroy;

        private bool _isDisposed;

        public StackInstancePool(Func<T> create, Action<T> destroy)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _destroy = destroy ?? throw new ArgumentNullException(nameof(destroy));
        }

        public T Get()
        {
            return _sleeping.Count > 0 ? _sleeping.Pop() : _create();
        }

        public void Release(T instance)
        {
            if (_isDisposed)
            {
                _destroy(instance);
                return;
            }

            _sleeping.Push(instance);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            while (_sleeping.Count > 0)
            {
                _destroy(_sleeping.Pop());
            }
        }
    }
}
