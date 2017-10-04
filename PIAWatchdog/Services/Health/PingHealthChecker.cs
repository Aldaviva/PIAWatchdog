using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using PIAWatchdog.Injection;
using PIAWatchdog.Properties;

namespace PIAWatchdog.Services.Health
{
    [Component]
    public class PingHealthChecker : HealthChecker
    {
        private readonly int timeout = Math.Min(Settings.Default.healthCheckIntervalWhileHealthy,
            Settings.Default.healthCheckIntervalWhileUnhealthy);
        
        public async Task<bool> IsHostHealthy(string host, CancellationToken cancellationToken)
        {
            using (var ping = new Ping())
            {
                cancellationToken.Register(ping.SendAsyncCancel);
                PingReply pingReply = await ping.SendPingAsync(host, timeout);
                return pingReply.Status == IPStatus.Success;
            }
        }

    }
}