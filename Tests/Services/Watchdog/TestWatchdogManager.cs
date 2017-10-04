using System;
using System.Collections.Generic;
using System.Threading;
using Autofac.Features.OwnedInstances;
using FakeItEasy;
using FluentAssertions;
using PIAWatchdog.Exceptions;
using PIAWatchdog.Properties;
using PIAWatchdog.Services.Health;
using PIAWatchdog.Services.Killing;
using PIAWatchdog.Services.Watchdog;
using Xunit;

namespace Tests.Services.Watchdog
{
    public class TestWatchdogManager : IDisposable
    {
        private readonly WatchdogManagerImpl watchdogManager;
        private HealthChecker healthChecker = A.Fake<HealthChecker>();
        private ProcessKiller processKiller = A.Fake<ProcessKiller>();
        private PIAWatchdog.Services.Watchdog.Watchdog watchdog = A.Fake<PIAWatchdog.Services.Watchdog.Watchdog>();

        public TestWatchdogManager()
        {
            Owned<PIAWatchdog.Services.Watchdog.Watchdog> WatchdogFactory() =>
                new Owned<PIAWatchdog.Services.Watchdog.Watchdog>(watchdog, this);

            watchdogManager = new WatchdogManagerImpl(healthChecker, processKiller, WatchdogFactory);

            Settings settings = Settings.Default;
            settings.healthCheckIntervalWhileHealthy = 100;
            settings.healthCheckPingHost = "1.2.3.4";
            settings.processesToKillOnOutage = new List<string> { "calc" };
        }

        [Fact]
        public void Start()
        {
            watchdogManager.Start();

            A.CallTo(() => watchdog.Start(A<CancellationToken>._)).MustHaveHappened();
            A.CallToSet(() => watchdog.HealthCheckIntervalWhileHealthy).To(TimeSpan.FromMilliseconds(100)).MustHaveHappened();
            A.CallToSet(() => watchdog.HostToWatch).To("1.2.3.4").MustHaveHappened();
            A.CallToSet(() => watchdog.ProcessesToKillOnHostDown)
                .To(() => A<ICollection<string>>.That.IsSameSequenceAs(new List<string> { "calc" })).MustHaveHappened();
        }

        [Fact]
        public void CannotStartWhenAlreadyStarted()
        {
            watchdogManager.Start();

            Action secondStart = () => watchdogManager.Start();
            secondStart.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void ValidateSettings()
        {
            Settings.Default.healthCheckIntervalWhileHealthy = 0;
            Action thrower = () => watchdogManager.Start();
            thrower.ShouldThrow<SettingsException>();
        }

        public void Dispose()
        {
            watchdogManager?.Dispose();
        }
    }
}