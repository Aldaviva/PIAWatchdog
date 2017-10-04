using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using PIAWatchdog.Services.Killing;
using Xunit;

namespace Tests.Services.Killing
{
    public class TestProcessKiller
    {
        private readonly ProcessKillerImpl processKiller = new ProcessKillerImpl();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        [Fact]
        public async void KillProcesses()
        {
            ICollection<Process> testProcesses = SpawnTestProcesses(2);

            testProcesses.Should().NotContain(process => process.HasExited, "haven't killed anything yet");

            await processKiller.KillProcess("calc", cancellationTokenSource.Token);

            testProcesses.Should().OnlyContain(process => process.HasExited, "all processes should have been killed");
        }

        [Fact]
        public void KillProcessAlreadyExited()
        {
            Process process = SpawnTestProcesses(1)[0];

            ProcessKillerImpl.KillProcess(process);
            ProcessKillerImpl.KillProcess(process);
        }
        
        [Fact]
        public void KillProcessNoPermissions()
        {
            Process systemIdleProcess = Process.GetProcessById(0);
            
            ProcessKillerImpl.KillProcess(systemIdleProcess);
        }

        private static IList<Process> SpawnTestProcesses(int howManyToSpawn)
        {
            var processes = new List<Process>();
            for (int i = 0; i < howManyToSpawn; i++)
            {
                var process = new Process();
                processes.Add(process);
                process.StartInfo = new ProcessStartInfo("calc.exe");
                process.Start();
            }
            return processes;
        }
    }
}