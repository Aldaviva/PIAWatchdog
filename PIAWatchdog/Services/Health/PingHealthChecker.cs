using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using PIAWatchdog.Injection;

namespace PIAWatchdog.Services.Health
{
    [Component]
    public class PingHealthChecker : HealthChecker
    {
        /// <summary>
        /// How long to wait for a ping response, in milliseconds.
        /// </summary>
        private const int TIMEOUT = 500;

        public async Task<bool> IsHostHealthy(string host, CancellationToken cancellationToken)
        {
            using (var ping = new Ping())
            {
                cancellationToken.Register(ping.SendAsyncCancel);
                PingReply pingReply = await ping.SendPingAsync(host, TIMEOUT);
                return pingReply.Status == IPStatus.Success;
            }
        }
    }
}