using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PIAWatchdog.Services.Enablement
{
    public interface ProcessEnabler
    {
        Task EnableProcesses(CancellationToken cancellationToken);
        Task DisableProcesses(CancellationToken cancellationToken);
        ICollection<string> Processes { get; set; }
    }
}