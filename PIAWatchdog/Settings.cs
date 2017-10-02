using PIAWatchdog.Exceptions;

namespace PIAWatchdog.Properties
{
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings
    {
        public Settings()
        {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(healthCheckPingHost))
            {
                throw new SettingsException("healthCheckPingHost", healthCheckPingHost,
                    "must be the host or IP address to ping, such as 10.10.10.1");
            }

            if (healthCheckInterval <= 0)
            {
                throw new SettingsException("healthCheckInterval", healthCheckInterval,
                    "must be the number of milliseconds between health checks, such as 5000");
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
        }

        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            // Add code to handle the SettingChangingEvent event here.
        }

        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Add code to handle the SettingsSaving event here.
        }
    }
}