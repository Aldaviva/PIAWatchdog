using System;
using System.Collections.Generic;
using System.Threading;
using Autofac.Features.OwnedInstances;
using FakeItEasy;
using FluentAssertions;
using PIAWatchdog.Exceptions;
using PIAWatchdog.Properties;
using PIAWatchdog.Services.Enablement;
using PIAWatchdog.Services.Health;
using PIAWatchdog.Services.Watchdog;
using Xunit;

namespace Tests.Services.Watchdog
{
    public class TestWatchdogManager : IDisposable
    {
        private readonly WatchdogManagerImpl watchdogManager;
        private ProcessEnabler processKiller = A.Fake<ProcessEnabler>();
        private PIAWatchdog.Services.Watchdog.Watchdog watchdog = A.Fake<PIAWatchdog.Services.Watchdog.Watchdog>();

        public TestWatchdogManager()
        {
            Owned<PIAWatchdog.Services.Watchdog.Watchdog> WatchdogFactory() =>
                new Owned<PIAWatchdog.Services.Watchdog.Watchdog>(watchdog, this);

            Owned<ProcessEnabler> ProcessEnablerFactory() => new Owned<ProcessEnabler>(processKiller, this);

            watchdogManager = new WatchdogManagerImpl(ProcessEnablerFactory, WatchdogFactory);

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
            secondStart.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ValidateSettings()
        {
            Settings.Default.healthCheckIntervalWhileHealthy = 0;
            Action thrower = () => watchdogManager.Start();
            thrower.Should().Throw<SettingsException>();
        }

        public void Dispose()
        {
            watchdogManager?.Dispose();
        }
    }
}