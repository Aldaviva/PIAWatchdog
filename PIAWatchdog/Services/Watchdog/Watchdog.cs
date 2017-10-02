using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PIAWatchdog.Injection;
using PIAWatchdog.Services.Health;
using PIAWatchdog.Services.Killing;

namespace PIAWatchdog.Services.Watchdog
{
    public interface Watchdog : IDisposable
    {
        string HostToWatch { get; set; }
        ICollection<string> ProcessesToKillOnHostDown { get; set; }
        TimeSpan HealthCheckInterval { get; set; }

        void Start(CancellationToken cancellationToken);
    }

    [Component]
    public class WatchdogImpl : Watchdog
    {
        private const int CONSECUTIVE_DOWN_FOR_OUTAGE = 3;

        private readonly HealthChecker healthChecker;
        private readonly ProcessKiller processKiller;

        public string HostToWatch { get; set; }
        public ICollection<string> ProcessesToKillOnHostDown { get; set; }
        public TimeSpan HealthCheckInterval { get; set; }

        private bool IsOutage => consecutiveHealthFailures >= CONSECUTIVE_DOWN_FOR_OUTAGE && !killedProcessesDuringCurrentOutage;

        private int consecutiveHealthFailures;
        private bool killedProcessesDuringCurrentOutage;
        private Task runner;

        public WatchdogImpl(HealthChecker healthChecker, ProcessKiller processKiller)
        {
            this.healthChecker = healthChecker;
            this.processKiller = processKiller;
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (runner != null)
            {
                throw new InvalidOperationException("Watchdog already started, call Stop() first.");
            }

            runner = Task.Factory.StartNew(async () => await CheckContinuously(cancellationToken), cancellationToken);
        }

        private async Task CheckContinuously(CancellationToken cancellationToken)
        {
            ResetHealthState();

            while (!cancellationToken.IsCancellationRequested)
            {
                await CheckOnce(cancellationToken);
                await Task.Delay(HealthCheckInterval, cancellationToken);
            }
        }

        internal async Task CheckOnce(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Checking if {HostToWatch} is healthy...");

            bool isHostHealthy = await healthChecker.IsHostHealthy(HostToWatch, cancellationToken);
            await OnHostHealthyOrDown(isHostHealthy, cancellationToken);
        }

        private async Task OnHostHealthyOrDown(bool isHostHealthy, CancellationToken cancellationToken)
        {
            if (isHostHealthy)
            {
                Console.WriteLine($"{HostToWatch} is up.");
                ResetHealthState();
            }
            else
            {
                Console.WriteLine($"{HostToWatch} is down.");
                consecutiveHealthFailures++;
            }

            if (IsOutage)
            {
                await OnHostOutage(cancellationToken);
            }
        }

        private async Task OnHostOutage(CancellationToken cancellationToken)
        {
            Console.WriteLine(
                $"{HostToWatch} has been down {CONSECUTIVE_DOWN_FOR_OUTAGE} times in a row, killing {string.Join(", ", ProcessesToKillOnHostDown)}");
            await Task.WhenAll(ProcessesToKillOnHostDown.Select(process =>
                processKiller.KillProcess(process, cancellationToken)));
            killedProcessesDuringCurrentOutage = true;
        }

        private void ResetHealthState()
        {
            consecutiveHealthFailures = 0;
            killedProcessesDuringCurrentOutage = false;
        }

        public void Dispose()
        {
            if (!runner.IsCanceled && !runner.IsCompleted && !runner.IsFaulted)
            {
                try
                {
                    runner.Wait();
                }
                catch (AggregateException)
                {
                    //dispose anyway
                }
            }
            runner.Dispose();
        }
    }
}