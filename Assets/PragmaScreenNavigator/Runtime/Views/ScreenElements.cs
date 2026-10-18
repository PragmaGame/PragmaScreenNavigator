using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The elements of a screen and their view models: registers the elements that land on the screen, hands them view
    /// models from the pool and passes the screen lifecycle on to them. An element may appear or leave in the middle of a
    /// pass (a view model adds a list row in OnShow), so every pass goes over a snapshot and skips removed entries, and a new
    /// element catches up with the screen right away.
    /// </summary>
    internal sealed class ScreenElements
    {
        private readonly List<Entry> _entries = new List<Entry>();

        private ElementViewModelPool _pool;
        private ScreenState _state;

        /// <summary>
        /// The navigation hands the pool over when it creates the screen. Without it (a screen in UI Builder) elements stay
        /// without view models.
        /// </summary>
        public void SetPool(ElementViewModelPool pool)
        {
            _pool = pool;
        }

        public void Register(ScreenElement element)
        {
            if (_pool == null)
            {
                return;
            }

            var type = element.ViewModelType;
            var viewModel = _pool.Rent(type);
            _entries.Add(new Entry(element, viewModel, type));

            viewModel.OnBind();
            element.Bind(viewModel);

            if (_state == ScreenState.Hidden)
            {
                return;
            }

            viewModel.OnShow();

            if (_state == ScreenState.Focused)
            {
                viewModel.OnFocus();
            }
        }

        public void Deregister(ScreenElement element)
        {
            var index = _entries.FindIndex(entry => entry.element == element);

            if (index < 0)
            {
                return;
            }

            var removed = _entries[index];
            _entries.RemoveAt(index);
            removed.isRemoved = true;

            if (_state == ScreenState.Focused)
            {
                removed.viewModel.OnBlur();
            }

            if (_state != ScreenState.Hidden)
            {
                removed.viewModel.OnHide();
            }

            element.Unbind();
            removed.viewModel.OnUnbind();
            _pool.Return(removed.type, removed.viewModel);
        }

        public void Show()
        {
            Notify(ScreenState.Visible, viewModel => viewModel.OnShow());
        }

        public void Focus()
        {
            Notify(ScreenState.Focused, viewModel => viewModel.OnFocus());
        }

        public void Blur()
        {
            Notify(ScreenState.Visible, viewModel => viewModel.OnBlur());
        }

        public void Hide()
        {
            Notify(ScreenState.Hidden, viewModel => viewModel.OnHide());
        }

        public void Update()
        {
            Notify(_state, viewModel => viewModel.OnUpdate());
        }

        /// <summary>
        /// The state changes before the pass: an element registered in the middle of the pass catches up with the new state
        /// and is not in the snapshot.
        /// </summary>
        private void Notify(ScreenState state, Action<ElementViewModel> action)
        {
            _state = state;

            if (_entries.Count == 0)
            {
                return;
            }

            var snapshot = ListPool<Entry>.Get();
            snapshot.AddRange(_entries);

            foreach (var entry in snapshot)
            {
                if (!entry.isRemoved)
                {
                    action(entry.viewModel);
                }
            }

            ListPool<Entry>.Release(snapshot);
        }

        private sealed class Entry
        {
            public readonly ScreenElement element;
            public readonly ElementViewModel viewModel;
            public readonly Type type;
            public bool isRemoved;

            public Entry(ScreenElement element, ElementViewModel viewModel, Type type)
            {
                this.element = element;
                this.viewModel = viewModel;
                this.type = type;
            }
        }
    }
}
