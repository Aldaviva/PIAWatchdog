using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PIAWatchdog.Services.Enablement
{
//    [Component]
    [Obsolete("Replaced with FirewallEnabler")]
    public class ProcessKillerImpl : ProcessEnabler
    {
        public ICollection<string> Processes { get; set; }

        public Task DisableProcesses(CancellationToken cancellationToken)
        {
            IEnumerable<Process> processes = Processes.SelectMany(Process.GetProcessesByName);

            return Task.WhenAll(processes.Select(process =>
                Task.Factory.StartNew(() => KillProcess(process), cancellationToken)));
        }

        public Task EnableProcesses(CancellationToken cancellationToken)
        {
            // do nothing, because we're not going to start the process here
            return Task.CompletedTask;
        }

        internal static void KillProcess(Process process)
        {
            string name = process.ProcessName;
            int pid = process.Id;

            try
            {
                Console.WriteLine($"Killing {name} ({pid})...");
                process.Kill();
                process.WaitForExit();
                Console.WriteLine($"Killed {name} ({pid}).");
            }
            catch (Win32Exception e)
            {
                Console.WriteLine(
                    $"Cannot kill {name} ({pid}) because it's still exiting or could not be terminated. " +
                    $"Error {e.NativeErrorCode}: {e.Message}");
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine($"Cannot kill {name} ({pid}) because it already exited. " +
                                  $"Error {e.HResult}: {e.Message}");
            }
        }
    }
}