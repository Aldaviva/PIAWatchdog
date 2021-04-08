using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsFirewallHelper;
using WindowsFirewallHelper.FirewallAPIv2.Rules;
using PIAWatchdog.Injection;

namespace PIAWatchdog.Services.Enablement
{
    [Component]
    public class FirewallEnabler : ProcessEnabler
    {
        private static readonly IFirewall Firewall = FirewallManager.Instance;
        private IEnumerable<IRule> processRules;
        private ICollection<string> processes;

        public FirewallEnabler()
        {
            Console.WriteLine("Creating new FirewallEnabler.");
        }

        public ICollection<string> Processes
        {
            get => processes;
            set
            {
                processes = value;
                InitProcessRules();
            }
        }

        public Task EnableProcesses(CancellationToken cancellationToken)
        {
            return ToggleProcesses(true);
        }

        public Task DisableProcesses(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Disabling all firewall rules for {string.Join(", ", Processes)}...");
            return ToggleProcesses(false);
        }

        private void InitProcessRules()
        {
            processRules = FindRules(Processes);
            Console.WriteLine($"Found {processRules.Count()} firewall rules for {string.Join(", ", Processes)}.");
        }

        private Task ToggleProcesses(bool shouldEnable)
        {
            try
            {
                foreach (IRule rule in processRules)
                {
                    var ruleAction = shouldEnable ? FirewallAction.Allow : FirewallAction.Block;
                    if (ruleAction != rule.Action)
                    {
                        Console.WriteLine($"Setting firewall rule {rule.Name} ({rule.Direction}) to {ruleAction}...");
                        rule.Action = ruleAction;
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine("Shit's broke, yo.");
                Console.WriteLine(e);
                throw;
            }
        }

        private static IEnumerable<IRule> FindRules(ICollection<string> processNames)
        {
//            Console.WriteLine("Finding existing firewall rules...");
            var allRules = Firewall.Rules.ToArray();
//            Console.WriteLine($"Found {allRules.Length} existing firewall rules, filtering for {processName} rules...");
            return allRules.Where(rule =>
            {
//                Console.WriteLine($"checking {rule.Name}...");
                string applicationName = (rule as StandardRule)?.ApplicationName;
                bool correctApplicationName =
                    processNames.Any(processName => applicationName?.EndsWith($"\\{processName}.exe") ?? false);
                return correctApplicationName && rule.IsEnable;
            });
        }
    }
}