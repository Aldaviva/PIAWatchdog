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
        TimeSpan HealthCheckIntervalWhileHealthy { get; set; }
        TimeSpan HealthCheckIntervalWhileUnhealthy { get; set; }
        int ConsecutiveDownForOutage { get; set; }

        void Start(CancellationToken cancellationToken);
    }

    [Component]
    public class WatchdogImpl : Watchdog
    {
        private readonly HealthChecker healthChecker;
        private readonly ProcessKiller processKiller;

        public string HostToWatch { get; set; }
        public ICollection<string> ProcessesToKillOnHostDown { get; set; }
        public TimeSpan HealthCheckIntervalWhileHealthy { get; set; }
        public TimeSpan HealthCheckIntervalWhileUnhealthy { get; set; }
        public int ConsecutiveDownForOutage { get; set; } = 3;

        private bool IsOutage => consecutiveHealthFailures >= ConsecutiveDownForOutage;

        private int consecutiveHealthFailures;

        internal Task Runner;

        public WatchdogImpl(HealthChecker healthChecker, ProcessKiller processKiller)
        {
            this.healthChecker = healthChecker;
            this.processKiller = processKiller;
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (Runner != null)
            {
                throw new InvalidOperationException("Watchdog already started, call Stop() first.");
            }

            Runner = Task.Factory.StartNew(async () => await CheckContinuously(cancellationToken), cancellationToken);
        }

        private async Task CheckContinuously(CancellationToken cancellationToken)
        {
            ResetHealthState();

            while (!cancellationToken.IsCancellationRequested)
            {
                bool isHealthy = await CheckOnce(cancellationToken);
                TimeSpan delayBeforeNextCheck = isHealthy ? HealthCheckIntervalWhileHealthy : HealthCheckIntervalWhileUnhealthy;
                await Task.Delay(delayBeforeNextCheck, cancellationToken);
            }
        }

        internal async Task<bool> CheckOnce(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Checking if {HostToWatch} is healthy...");

            bool isHostHealthy = await healthChecker.IsHostHealthy(HostToWatch, cancellationToken);
            await OnHostHealthyOrDown(isHostHealthy, cancellationToken);
            return isHostHealthy;
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
                $"Outage! {HostToWatch} has been down {consecutiveHealthFailures} times in a row, killing {string.Join(", ", ProcessesToKillOnHostDown)}");
            await Task.WhenAll(ProcessesToKillOnHostDown.Select(process =>
                processKiller.KillProcess(process, cancellationToken)));
        }

        private void ResetHealthState()
        {
            consecutiveHealthFailures = 0;
        }

        public void Dispose()
        {
            if (Runner != null && !Runner.IsCanceled && !Runner.IsCompleted && !Runner.IsFaulted)
            {
                try
                {
                    Runner.Wait();
                }
                catch (AggregateException)
                {
                    //dispose anyway
                }
            }
            Runner?.Dispose();
        }
    }
}