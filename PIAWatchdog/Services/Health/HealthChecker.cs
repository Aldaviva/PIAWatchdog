using System.Threading;
using System.Threading.Tasks;

namespace PIAWatchdog.Services.Health
{
    public interface HealthChecker
    {
        Task<bool> IsHostHealthy(string host, CancellationToken cancellationToken);
    }
}