using System;
using System.Collections.Generic;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// Element view models, one pool per type: an element takes a view model of its type, an element leaving the screen
    /// returns it, and <see cref="IViewModelFactory"/> creates new ones. One per navigation: elements of different screens
    /// share it.
    /// </summary>
    internal sealed class ElementViewModelPool : IDisposable
    {
        private readonly Dictionary<Type, IInstancePool<ElementViewModel>> _pools = new Dictionary<Type, IInstancePool<ElementViewModel>>();
        private readonly IViewModelFactory _factory;
        private readonly IInstancePoolFactory _poolFactory;
        private readonly Action<ElementViewModel> _destroy = Destroy;

        public ElementViewModelPool(IViewModelFactory factory, IInstancePoolFactory poolFactory)
        {
            _factory = factory;
            _poolFactory = poolFactory;
        }

        public ElementViewModel Rent(Type type)
        {
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = _poolFactory.Create(() => Create(type), _destroy);
                _pools.Add(type, pool);
            }

            return pool.Get();
        }

        public void Return(Type type, ElementViewModel viewModel)
        {
            _pools[type].Release(viewModel);
        }

        public void Dispose()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
        }

        private ElementViewModel Create(Type type)
        {
            var viewModel = _factory.Create(type);

            if (!(viewModel is ElementViewModel elementViewModel) || !type.IsInstanceOfType(viewModel))
            {
                throw new InvalidOperationException($"The view model factory returned {viewModel?.GetType().Name} instead of {type.Name}.");
            }

            return elementViewModel;
        }

        private static void Destroy(ElementViewModel viewModel)
        {
            viewModel.OnDestroy();
        }
    }
}
