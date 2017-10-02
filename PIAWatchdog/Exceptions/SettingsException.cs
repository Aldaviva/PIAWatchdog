using System;

namespace PIAWatchdog.Exceptions
{
    [Serializable]
    internal class SettingsException : Exception
    {
        public string SettingsKey { get; }
        public object InvalidValue { get; }

        public SettingsException(string settingsKey, object invalidValue, string message) : base(message)
        {
            SettingsKey = settingsKey;
            InvalidValue = invalidValue;
        }
    }
}