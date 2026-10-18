using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// A data source for UI Toolkit bindings: data is exposed as [CreateProperty] properties and changed through
    /// <see cref="SetProperty{T}"/>, or announced as changed through <see cref="NotifyPropertyChanged"/>. Screen and element
    /// view models derive from it; a part of a screen with its own data but no lifecycle of its own (a list row) can derive
    /// from it directly, and the view makes it the dataSource of its element.
    ///
    /// A concrete view model needs [GeneratePropertyBag]: the property bag is generated instead of being built through
    /// reflection, which IL2CPP strips.
    /// </summary>
    public abstract class ViewModel : INotifyBindablePropertyChanged, IDataSourceViewHashProvider
    {
        private long _version;

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        /// <summary>Changes with every notification: the binding system compares it to decide whether to update the elements.</summary>
        public long GetViewHashCode()
        {
            return _version;
        }

        /// <summary>Writes the value and notifies the bindings, only if the value changed.</summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string property = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            NotifyPropertyChanged(property);

            return true;
        }

        /// <summary>Announces the property as changed: for computed properties that store no value.</summary>
        protected void NotifyPropertyChanged([CallerMemberName] string property = null)
        {
            _version++;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
