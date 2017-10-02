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
            new object[] { "healthCheckInterval", 500 },
            new object[] { "healthCheckInterval", 5000 },
            new object[] { "processesToKillOnOutage", new List<string>{"calc"} },
            new object[] { "processesToKillOnOutage", new List<string>{"calc", "notepad"} }
        };

        public static IEnumerable<object[]> InvalidSettings => new[]
        {
            new object[] { "healthCheckPingHost", " " },
            new object[] { "healthCheckPingHost", "" },
            new object[] { "healthCheckPingHost", null },
            new object[] { "healthCheckInterval", 0 },
            new object[] { "healthCheckInterval", -1 },
            new object[] { "processesToKillOnOutage", null},
            new object[] { "processesToKillOnOutage", new List<string>() },
            new object[] { "processesToKillOnOutage", new List<string>{""} },
            new object[] { "processesToKillOnOutage", new List<string>{" "} },
            new object[] { "processesToKillOnOutage", new List<string>{"calc.exe"} },
            new object[] { "processesToKillOnOutage", new List<string>{"calc.exe", "notepad.exe"} },
            new object[] { "processesToKillOnOutage", new List<string>{"calc.exe", "notepad"} },
            new object[] { "processesToKillOnOutage", new List<string>{"calc", "notepad.exe"} }
        };

        [Fact]
        public void AllSettingsCovered()
        {
            foreach (SettingsProperty settingsProperty in new Settings().Properties)
            {
                string name = settingsProperty.Name;
                ValidSettings.Should().Contain(theories => theories[0].Equals(name), $"ValidSettings must test the {name} setting");
                InvalidSettings.Should().Contain(theories => theories[0].Equals(name), $"InvalidSettings must test the {name} setting");
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
                validateMethod.ShouldNotThrow<SettingsException>();
            }
            else
            {
                validateMethod.ShouldThrow<SettingsException>();
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
                healthCheckInterval = 1000
            };
            settings.Validate();
            return settings;
        }
    }
}