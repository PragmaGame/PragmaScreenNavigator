using System.Collections.Generic;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>
    /// The test log: lifecycle calls of the view models in order ("First.Show") and the created view models. View models get
    /// it through the constructor from <see cref="TestViewModelFactory"/>.
    /// </summary>
    public sealed class TestLog
    {
        private readonly List<string> _entries = new List<string>();

        public List<ViewModel> Created { get; } = new List<ViewModel>();

        public void Add(string entry)
        {
            _entries.Add(entry);
        }

        /// <summary>The entries since the previous call.</summary>
        public string[] Take()
        {
            var entries = _entries.ToArray();
            _entries.Clear();

            return entries;
        }
    }
}
