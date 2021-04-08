using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PIAWatchdog.Injection;
using PIAWatchdog.Services.Enablement;
using PIAWatchdog.Services.Health;

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
        private readonly ProcessEnabler processKiller;

        public string HostToWatch { get; set; }
        public ICollection<string> ProcessesToKillOnHostDown { get; set; }
        public TimeSpan HealthCheckIntervalWhileHealthy { get; set; }
        public TimeSpan HealthCheckIntervalWhileUnhealthy { get; set; }
        public int ConsecutiveDownForOutage { get; set; } = 3;

        private bool IsOutage => consecutiveHealthFailures >= ConsecutiveDownForOutage;

        private int consecutiveHealthFailures;

        internal Task Runner;

        public WatchdogImpl(HealthChecker healthChecker, ProcessEnabler processKiller)
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
            try
            {
                ResetHealthState();

                while (!cancellationToken.IsCancellationRequested)
                {
                    bool isHealthy = await CheckOnce(cancellationToken);
                    TimeSpan delayBeforeNextCheck = isHealthy ? HealthCheckIntervalWhileHealthy : HealthCheckIntervalWhileUnhealthy;
                    await Task.Delay(delayBeforeNextCheck, cancellationToken);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Oh no! " + e);
                throw;
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
            var wasOutage = IsOutage;

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
            else if (wasOutage)
            {
                await OnHostOutageOver(cancellationToken);
            }
        }

        private async Task OnHostOutage(CancellationToken cancellationToken)
        {
            Console.WriteLine(
                $"Outage! {HostToWatch} has been down {consecutiveHealthFailures} times in a row, killing {string.Join(", ", ProcessesToKillOnHostDown)}.");
            await processKiller.DisableProcesses(cancellationToken);
        }

        private async Task OnHostOutageOver(CancellationToken cancellationToken)
        {
            Console.WriteLine($"The outage is over. Reenabling {string.Join(", ", ProcessesToKillOnHostDown)}.");
            await processKiller.EnableProcesses(cancellationToken);
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