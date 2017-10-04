using PIAWatchdog.Exceptions;

// ReSharper disable once CheckNamespace
namespace PIAWatchdog.Properties
{
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings
    {
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(healthCheckPingHost))
            {
                throw new SettingsException("healthCheckPingHost", healthCheckPingHost,
                    "must be the host or IP address to ping, such as 10.10.10.1");
            }

            if (healthCheckIntervalWhileHealthy <= 0)
            {
                throw new SettingsException("healthCheckInterval", healthCheckIntervalWhileHealthy,
                    "must be the number of milliseconds between health checks if the healthCheckPingHost was healthy after the most recent check, such as 5000");
            }
            
            if (healthCheckIntervalWhileUnhealthy <= 0)
            {
                throw new SettingsException("healthCheckIntervalWhileUnhealthy", healthCheckIntervalWhileUnhealthy,
                    "must be the number of milliseconds between health checks if the healthCheckPingHost was unhealthy after the most recent check, such as 5000");
            }

            if (processesToKillOnOutage == null || processesToKillOnOutage.Count == 0)
            {
                throw new SettingsException("processesToKillOnDown", processesToKillOnOutage,
                    "must specify the process(es) to kill when the host is down");
            }

            for (int i = 0; i < processesToKillOnOutage.Count; i++)
            {
                string processToKill = processesToKillOnOutage[i];
                if (string.IsNullOrWhiteSpace(processToKill))
                {
                    throw new SettingsException($"processesToKillOnDown[{i}]", processToKill,
                        "must specify the name of the process to kill, such as iexplore");
                }
                if (processToKill.EndsWith(".exe"))
                {
                    throw new SettingsException($"processesToKillOnDown[{i}]", processToKill,
                        "name of process to kill must not end with \".exe\"");
                }
            }

            if (consecutiveDownForOutage <= 0)
            {
                throw new SettingsException("consecutiveDownForOutage", consecutiveDownForOutage,
                    "must be the number of times the healthCheckPingHost is down in a row before the processesToKillOnOutage are killed");
            }
        }
    }
}