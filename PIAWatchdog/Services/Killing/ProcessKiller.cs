using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PIAWatchdog.Injection;

namespace PIAWatchdog.Services.Killing
{
    public interface ProcessKiller
    {
        Task KillProcess(string processName, CancellationToken cancellationToken);
    }

    [Component]
    public class ProcessKillerImpl : ProcessKiller
    {
        public Task KillProcess(string processName, CancellationToken cancellationToken)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            return Task.WhenAll(processes.Select(process =>
                Task.Factory.StartNew(() => KillProcess(process), cancellationToken)));
        }

        private static void KillProcess(Process process)
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