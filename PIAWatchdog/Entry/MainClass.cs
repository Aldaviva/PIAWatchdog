using System;
using System.Linq;
using System.ServiceProcess;
using Autofac;
using PIAWatchdog.Injection;
using PIAWatchdog.Services;
using PIAWatchdog.Services.Watchdog;

namespace PIAWatchdog.Entry
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Contains("--console"))
            {
                using (IContainer container = ContainerFactory.CreateContainer())
                using (ILifetimeScope scope = container.BeginLifetimeScope())
                {
                    var watchdog = scope.Resolve<WatchdogManager>();
                    watchdog.Start();
                    
                    Console.WriteLine("Running. Press any key to exit.");
                    Console.Read();
                }
            }
            else
            {
                ServiceBase.Run(new WindowsService());
            }
        }
    }
}