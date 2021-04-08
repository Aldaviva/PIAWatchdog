using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using PIAWatchdog.Exceptions;
using PIAWatchdog.Properties;
using Xunit;

namespace Tests
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class TestSettings
    {
        public static IEnumerable<object[]> ValidSettings => new[]
        {
            new object[] { "healthCheckPingHost", "1.2.3.4" },
            new object[] { "healthCheckPingHost", "google.com" },
            new object[] { "healthCheckIntervalWhileHealthy", 500 },
            new object[] { "healthCheckIntervalWhileHealthy", 5000 },
            new object[] { "healthCheckIntervalWhileUnhealthy", 500 },
            new object[] { "healthCheckIntervalWhileUnhealthy", 5000 },
            new object[] { "consecutiveDownForOutage", 1 },
            new object[] { "consecutiveDownForOutage", 5 },
            new object[] { "processesToKillOnOutage", new List<string> { "calc" } },
            new object[] { "processesToKillOnOutage", new List<string> { "calc", "notepad" } }
        };

        public static IEnumerable<object[]> InvalidSettings => new[]
        {
            new object[] { "healthCheckPingHost", " " },
            new object[] { "healthCheckPingHost", "" },
            new object[] { "healthCheckPingHost", null },
            new object[] { "healthCheckIntervalWhileHealthy", 0 },
            new object[] { "healthCheckIntervalWhileHealthy", -1 },
            new object[] { "healthCheckIntervalWhileUnhealthy", 0 },
            new object[] { "healthCheckIntervalWhileUnhealthy", -1 },
            new object[] { "consecutiveDownForOutage", 0 },
            new object[] { "consecutiveDownForOutage", -1 },
            new object[] { "processesToKillOnOutage", null },
            new object[] { "processesToKillOnOutage", new List<string>() },
            new object[] { "processesToKillOnOutage", new List<string> { null } },
            new object[] { "processesToKillOnOutage", new List<string> { "" } },
            new object[] { "processesToKillOnOutage", new List<string> { " " } },
            new object[] { "processesToKillOnOutage", new List<string> { "calc.exe" } },
            new object[] { "processesToKillOnOutage", new List<string> { "calc.exe", "notepad.exe" } },
            new object[] { "processesToKillOnOutage", new List<string> { "calc.exe", "notepad" } },
            new object[] { "processesToKillOnOutage", new List<string> { "calc", "notepad.exe" } }
        };

        [Fact]
        public void AllSettingsCovered()
        {
            foreach (SettingsProperty settingsProperty in new Settings().Properties)
            {
                string name = settingsProperty.Name;
                ValidSettings.Should().Contain(theories => theories[0].Equals(name), $"ValidSettings must test the {name} setting");
                InvalidSettings.Should().Contain(theories => theories[0].Equals(name),
                    $"InvalidSettings must test the {name} setting");
            }
        }

        [Theory, MemberData(nameof(ValidSettings))]
        public void ValidateShouldnotThrowOnValidSettings(string settingsKey, object settingsValue)
        {
            TestSettingsValidation(settingsKey, settingsValue, true);
        }

        [Theory, MemberData(nameof(InvalidSettings))]
        public void ValidateShouldThrowOnInvalidSettings(string settingsKey, object settingsValue)
        {
            TestSettingsValidation(settingsKey, settingsValue, false);
        }

        private static void TestSettingsValidation(string settingsKey, object settingsValue, bool isValid)
        {
            Settings settings = CreateValidSettings();
            settings[settingsKey] = settingsValue;
            Action validateMethod = settings.Validate;
            if (isValid)
            {
                validateMethod.Should().NotThrow<SettingsException>();
            }
            else
            {
                validateMethod.Should().Throw<SettingsException>();
            }
        }

        private static Settings CreateValidSettings()
        {
            var settings = new Settings
            {
                processesToKillOnOutage = new List<string>
                {
                    "calc",
                    "notepad"
                },
                healthCheckPingHost = "1.2.3.4",
                healthCheckIntervalWhileHealthy = 1000,
                consecutiveDownForOutage = 3,
                healthCheckIntervalWhileUnhealthy = 500
            };
            settings.Validate();
            return settings;
        }
    }
}