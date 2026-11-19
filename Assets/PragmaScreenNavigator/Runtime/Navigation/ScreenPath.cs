using System;

namespace Pragma.ScreenNavigator
{
    /// <summary>
    /// A path of screens: the screens to open in order over what is open, each with its open arguments if it has them, so
    /// Back walks the way the player came. Keep one to come back later — after a scene change, a battle, a reload — and
    /// open it once the navigation is there (<see cref="Open"/>). A step is a navigation command: push a screen, with
    /// arguments of their own type (<see cref="IScreenArgs{TArgs}"/>). The path is immutable: <see cref="Then(TScreen)"/>
    /// returns a new one, a step longer, so a path can be kept and handed around without being extended behind one's back.
    /// </summary>
    public sealed class ScreenPath<TScreen>
        where TScreen : struct, Enum
    {
        private readonly Action<INavigation<TScreen>>[] _steps;

        private ScreenPath(Action<INavigation<TScreen>>[] steps)
        {
            _steps = steps;
        }

        /// <summary>The number of screens the path opens.</summary>
        public int Length => _steps.Length;

        /// <summary>A path of one screen.</summary>
        public static ScreenPath<TScreen> To(TScreen screen)
        {
            return new ScreenPath<TScreen>(Array.Empty<Action<INavigation<TScreen>>>()).Then(screen);
        }

        /// <summary>A path of one screen, with its open arguments.</summary>
        public static ScreenPath<TScreen> To<TArgs>(TScreen screen, TArgs args)
        {
            return new ScreenPath<TScreen>(Array.Empty<Action<INavigation<TScreen>>>()).Then(screen, args);
        }

        /// <summary>This path and one more screen over it.</summary>
        public ScreenPath<TScreen> Then(TScreen screen)
        {
            return Append(navigation => navigation.Push(screen));
        }

        /// <summary>This path and one more screen over it, with its open arguments.</summary>
        public ScreenPath<TScreen> Then<TArgs>(TScreen screen, TArgs args)
        {
            return Append(navigation => navigation.Push(screen, args));
        }

        /// <summary>Opens the screens of the path in order over what is open.</summary>
        public void Open(INavigation<TScreen> navigation)
        {
            if (navigation == null)
            {
                throw new ArgumentNullException(nameof(navigation));
            }

            foreach (var step in _steps)
            {
                step(navigation);
            }
        }

        private ScreenPath<TScreen> Append(Action<INavigation<TScreen>> step)
        {
            var steps = new Action<INavigation<TScreen>>[_steps.Length + 1];
            Array.Copy(_steps, steps, _steps.Length);
            steps[_steps.Length] = step;

            return new ScreenPath<TScreen>(steps);
        }
    }
}
