using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// The screens of an <see cref="INavigation{TScreen}"/>: one row per screen, its enum id, its UXML and its
    /// <see cref="ScreenMode"/>. The UXML root is
    /// the screen view (<see cref="ScreenView{TViewModel}"/>), and the navigation takes the view model type from the view, so
    /// the catalog stores no types. A concrete catalog is a subclass with its own enum and a CreateAssetMenu.
    /// </summary>
    public abstract class ScreenCatalog<TScreen> : ScriptableObject where TScreen : struct, Enum
    {
        [SerializeField, Tooltip("Screens and their UXML: the UXML root is the screen view (ScreenView). One row per screen.")]
        private List<ScreenEntry<TScreen>> _screens = new List<ScreenEntry<TScreen>>();

        /// <summary>The row of the screen; false when it has no row or no UXML.</summary>
        public bool TryGetEntry(TScreen screen, out ScreenEntry<TScreen> entry)
        {
            foreach (var candidate in _screens)
            {
                if (EqualityComparer<TScreen>.Default.Equals(candidate.Screen, screen))
                {
                    entry = candidate.View != null ? candidate : null;
                    return entry != null;
                }
            }

            entry = null;
            return false;
        }

        private void OnValidate()
        {
            var screens = new HashSet<TScreen>();

            foreach (var entry in _screens)
            {
                if (!screens.Add(entry.Screen))
                {
                    Debug.LogWarning($"{name}: screen {entry.Screen} has several rows, the first one is used.", this);
                }

                if (entry.View == null)
                {
                    Debug.LogWarning($"{name}: screen {entry.Screen} has no UXML assigned.", this);
                }
            }
        }
    }

    [Serializable]
    public sealed class ScreenEntry<TScreen> where TScreen : struct, Enum
    {
        [SerializeField, Tooltip("The screen: the enum value the navigation opens it by.")]
        private TScreen _screen;

        [SerializeField, Tooltip("The UXML of the screen: its root is the screen view (ScreenView).")]
        private VisualTreeAsset _view;

        [SerializeField, Tooltip("Overlay: the screens below stay visible (dialogs, popups). Page: covers them, so they hide once its show animation ends.")]
        private ScreenMode _mode;

        public TScreen Screen => _screen;

        public VisualTreeAsset View => _view;

        public ScreenMode Mode => _mode;
    }
}
