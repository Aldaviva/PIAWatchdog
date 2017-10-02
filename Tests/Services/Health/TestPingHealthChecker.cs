using System.Threading;
using FluentAssertions;
using PIAWatchdog.Services.Health;
using Xunit;

namespace Tests.Services.Health
{
    public class TestPingHealthChecker
    {
        private readonly PingHealthChecker pingHealthChecker = new PingHealthChecker();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        
        [Fact]
        public async void Up()
        {
            bool actual = await pingHealthChecker.IsHostHealthy("127.0.0.1", cancellationTokenSource.Token);
            actual.Should().BeTrue();
        }

        [Fact]
        public async void Down()
        {
            bool actual = await pingHealthChecker.IsHostHealthy("255.255.255.255", cancellationTokenSource.Token);
            actual.Should().BeFalse();
        }
    }
}