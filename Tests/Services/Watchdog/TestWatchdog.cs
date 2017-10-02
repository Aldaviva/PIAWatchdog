﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using PIAWatchdog.Services.Health;
using PIAWatchdog.Services.Killing;
using PIAWatchdog.Services.Watchdog;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Services.Watchdog
{
    public class TestWatchdog
    {
        private const int CONSECUTIVE_DOWN_FOR_OUTAGE = 3;

        private readonly WatchdogImpl watchdog;
        private readonly HealthChecker healthChecker = A.Fake<HealthChecker>();
        private readonly ProcessKiller processKiller = A.Fake<ProcessKiller>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public TestWatchdog()
        {
            watchdog = new WatchdogImpl(healthChecker, processKiller)
            {
                HostToWatch = "1.2.3.4",
                ProcessesToKillOnHostDown = new List<string> { "calc" },
                HealthCheckInterval = TimeSpan.FromMilliseconds(50)
            };
        }

        [Fact]
        public async void CheckOnceHealthyDoesNotKill()
        {
            A.CallTo(() => healthChecker.IsHostHealthy(A<string>._, A<CancellationToken>._)).Returns(true);

            for (int i = 0; i < CONSECUTIVE_DOWN_FOR_OUTAGE * 2; i++)
            {
                await watchdog.CheckOnce(cancellationTokenSource.Token);
            }

            A.CallTo(() => healthChecker.IsHostHealthy("1.2.3.4", cancellationTokenSource.Token))
                .MustHaveHappened(Repeated.Exactly.Times(CONSECUTIVE_DOWN_FOR_OUTAGE * 2));
            A.CallTo(() => processKiller.KillProcess(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Fact]
        public async void CheckOnceUnhealthyKills()
        {
            A.CallTo(() => healthChecker.IsHostHealthy(A<string>._, A<CancellationToken>._)).Returns(false);

            for (int i = 0; i < CONSECUTIVE_DOWN_FOR_OUTAGE * 2; i++)
            {
                await watchdog.CheckOnce(cancellationTokenSource.Token);
            }

            A.CallTo(() => healthChecker.IsHostHealthy("1.2.3.4", cancellationTokenSource.Token))
                .MustHaveHappened(Repeated.Exactly.Times(CONSECUTIVE_DOWN_FOR_OUTAGE * 2));
            A.CallTo(() => processKiller.KillProcess("calc", A<CancellationToken>._)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public async void CheckOnceHealthyResetsOutage()
        {
            bool[] isHealthyResponses = { false, false, false, true, false, false, false };
            A.CallTo(() => healthChecker.IsHostHealthy(A<string>._, A<CancellationToken>._))
                .ReturnsNextFromSequence(isHealthyResponses);

            for (int i = 0; i < isHealthyResponses.Length; i++)
            {
                await watchdog.CheckOnce(cancellationTokenSource.Token);
            }

            A.CallTo(() => healthChecker.IsHostHealthy("1.2.3.4", cancellationTokenSource.Token))
                .MustHaveHappened(Repeated.Exactly.Times(isHealthyResponses.Length));
            A.CallTo(() => processKiller.KillProcess("calc", A<CancellationToken>._)).MustHaveHappened(Repeated.Exactly.Twice);
        }

        [Fact]
        public void Start()
        {
            const int maxChecks = 3;
            int completedChecks = 0;

            A.CallTo(() => healthChecker.IsHostHealthy(A<string>._, A<CancellationToken>._)).ReturnsLazily(() =>
            {
                if (++completedChecks >= maxChecks)
                {
                    Task.Factory.StartNew(cancellationTokenSource.Cancel);
                }
                return true;
            });

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            watchdog.Start(cancellationTokenSource.Token);

            cancellationTokenSource.Token.WaitHandle.WaitOne();
            stopwatch.Stop();

            A.CallTo(() => healthChecker.IsHostHealthy("1.2.3.4", cancellationTokenSource.Token))
                .MustHaveHappened(Repeated.Exactly.Times(maxChecks));

            stopwatch.ElapsedMilliseconds.Should().BeInRange(
                (long) (watchdog.HealthCheckInterval.TotalMilliseconds * (maxChecks - 1)),
                (long) (watchdog.HealthCheckInterval.TotalMilliseconds * (maxChecks+1)),
                "should have delayed after each check (except last one, because we cancelled before the last delay)");
        }

        [Fact]
        public void CannotStartWhenAlreadyStarted()
        {
            A.CallTo(() => healthChecker.IsHostHealthy(A<string>._, A<CancellationToken>._)).Returns(true);
            
            watchdog.Start(cancellationTokenSource.Token);
            try
            {
                Action secondStart = () => watchdog.Start(cancellationTokenSource.Token);
                secondStart.ShouldThrow<InvalidOperationException>();
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }
        }
    }
}