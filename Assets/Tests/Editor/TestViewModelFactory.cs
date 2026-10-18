using System;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>Stands in for a DI container: every test view model takes the <see cref="TestLog"/> in its constructor.</summary>
    public sealed class TestViewModelFactory : IViewModelFactory
    {
        private readonly TestLog _log;

        public TestViewModelFactory(TestLog log)
        {
            _log = log;
        }

        public ViewModel Create(Type type)
        {
            return (ViewModel)Activator.CreateInstance(type, _log);
        }
    }
}
