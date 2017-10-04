using System;
using System.Threading;
using Autofac.Features.OwnedInstances;
using PIAWatchdog.Exceptions;
using PIAWatchdog.Injection;
using PIAWatchdog.Properties;
using PIAWatchdog.Services.Health;
using PIAWatchdog.Services.Killing;

namespace PIAWatchdog.Services.Watchdog
{
    public interface WatchdogManager : IDisposable
    {
        void Start();
        void Stop();
    }

    [Component]
    public class WatchdogManagerImpl : WatchdogManager
    {
        private readonly Func<Owned<Watchdog>> watchdogFactory;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Owned<Watchdog> watchdog;

        public WatchdogManagerImpl(HealthChecker healthChecker, ProcessKiller processKiller, Func<Owned<Watchdog>> watchdogFactory)
        {
            this.watchdogFactory = watchdogFactory;
        }

        public void Start()
        {
            if (watchdog != null)
            {
                throw new InvalidOperationException("WatchdogManager is already started, call Stop() first.");
            }
            Settings settings = Settings.Default;
            ValidateSettings(settings);

            watchdog = watchdogFactory();
            watchdog.Value.HealthCheckIntervalWhileHealthy = TimeSpan.FromMilliseconds(settings.healthCheckIntervalWhileHealthy);
            watchdog.Value.HealthCheckIntervalWhileUnhealthy = TimeSpan.FromMilliseconds(settings.healthCheckIntervalWhileUnhealthy);
            watchdog.Value.HostToWatch = settings.healthCheckPingHost;
            watchdog.Value.ProcessesToKillOnHostDown = settings.processesToKillOnOutage;
            watchdog.Value.ConsecutiveDownForOutage = settings.consecutiveDownForOutage;

            watchdog.Value.Start(cancellationTokenSource.Token);
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            watchdog?.Dispose();
            watchdog = null;
        }

        private static void ValidateSettings(Settings settings)
        {
            try
            {
                settings.Validate();
            }
            catch (SettingsException e)
            {
                Console.WriteLine($"Invalid setting: {e.SettingsKey} = {e.InvalidValue}");
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public void Dispose()
        {
            Stop();
            cancellationTokenSource?.Dispose();
        }
    }
}