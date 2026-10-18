using System;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>Logs every screen lifecycle call as "Name.Call".</summary>
    public abstract class LoggedScreenViewModel : ScreenViewModel
    {
        private readonly TestLog _log;
        private readonly string _name;

        protected LoggedScreenViewModel(TestLog log, string name)
        {
            _log = log;
            _name = name;
            log.Created.Add(this);
        }

        public bool IsClosedByBack { get; set; } = true;

        public int Updates { get; private set; }

        public void CloseSelf()
        {
            Close();
        }

        public void Touch(string property)
        {
            NotifyPropertyChanged(property);
        }

        protected override void OnCreate()
        {
            Log("Create");
        }

        protected override void OnShow()
        {
            Log("Show");
        }

        protected override void OnFocus()
        {
            Log("Focus");
        }

        protected override void OnBlur()
        {
            Log("Blur");
        }

        protected override void OnHide()
        {
            Log("Hide");
        }

        protected override void OnUpdate()
        {
            Updates++;
        }

        protected override bool OnBack()
        {
            Log("Back");

            return IsClosedByBack;
        }

        protected override void OnDestroy()
        {
            Log("Destroy");
        }

        protected void Log(string call)
        {
            _log.Add(_name + "." + call);
        }
    }

    public sealed class FirstTestViewModel : LoggedScreenViewModel
    {
        public FirstTestViewModel(TestLog log) : base(log, "First")
        {
        }
    }

    public sealed class SecondTestViewModel : LoggedScreenViewModel
    {
        public SecondTestViewModel(TestLog log) : base(log, "Second")
        {
        }
    }

    public sealed class DialogTestViewModel : LoggedScreenViewModel, IScreenArgs<string>
    {
        public const string THROWING_ARGS = "throw";

        public DialogTestViewModel(TestLog log) : base(log, "Dialog")
        {
        }

        public string Text { get; private set; }

        public void SetArgs(string args)
        {
            if (args == THROWING_ARGS)
            {
                throw new InvalidOperationException("The dialog refused its args.");
            }

            Text = args;
            Log("Args " + args);
        }
    }

    public sealed class PageTestViewModel : LoggedScreenViewModel
    {
        public PageTestViewModel(TestLog log) : base(log, "Page")
        {
        }
    }

    /// <summary>The view model of a screen whose view fails to bind (<see cref="BrokenTestScreen"/>).</summary>
    public sealed class BrokenTestViewModel : LoggedScreenViewModel
    {
        public BrokenTestViewModel(TestLog log) : base(log, "Broken")
        {
        }
    }

    public sealed class AnimatedTestViewModel : LoggedScreenViewModel
    {
        public AnimatedTestViewModel(TestLog log) : base(log, "Animated")
        {
        }
    }

    /// <summary>An element view model: logs its calls and counts frames.</summary>
    public sealed class CounterViewModel : ElementViewModel
    {
        private readonly TestLog _log;

        public CounterViewModel(TestLog log)
        {
            _log = log;
            log.Created.Add(this);
        }

        public int Updates { get; private set; }

        public void Touch(string property)
        {
            NotifyPropertyChanged(property);
        }

        protected override void OnBind()
        {
            _log.Add("Counter.Bind");
        }

        protected override void OnUnbind()
        {
            Updates = 0;
            _log.Add("Counter.Unbind");
        }

        protected override void OnShow()
        {
            _log.Add("Counter.Show");
        }

        protected override void OnFocus()
        {
            _log.Add("Counter.Focus");
        }

        protected override void OnBlur()
        {
            _log.Add("Counter.Blur");
        }

        protected override void OnHide()
        {
            _log.Add("Counter.Hide");
        }

        protected override void OnUpdate()
        {
            Updates++;
        }

        protected override void OnDestroy()
        {
            _log.Add("Counter.Destroy");
        }
    }

    /// <summary>A view model with a parameterless constructor, for <see cref="ActivatorViewModelFactory"/>.</summary>
    public sealed class PlainTestViewModel : ScreenViewModel
    {
    }
}
