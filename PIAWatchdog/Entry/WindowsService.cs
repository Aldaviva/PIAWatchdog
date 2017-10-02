using System.ServiceProcess;
using Autofac;
using PIAWatchdog.Injection;
using PIAWatchdog.Services.Watchdog;

namespace PIAWatchdog.Entry
{
    public partial class WindowsService : ServiceBase
    {
        private IContainer container;
        private ILifetimeScope scope;
        private WatchdogManager watchdogManager;

        public WindowsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            container = ContainerFactory.CreateContainer();
            scope = container.BeginLifetimeScope();

            watchdogManager = scope.Resolve<WatchdogManager>();
            watchdogManager.Start();
        }

        protected override void OnStop()
        {
            scope.Dispose();
            scope = null;

            container.Dispose();
            container = null;
        }
    }
}