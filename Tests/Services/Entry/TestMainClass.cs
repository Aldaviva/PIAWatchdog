using System;
using System.Collections.Generic;
using System.IO;
using PIAWatchdog.Entry;
using PIAWatchdog.Properties;
using Xunit;

namespace Tests.Services.Entry
{
    public class TestMainClass
    {
        public TestMainClass()
        {
            Settings.Default.healthCheckPingHost = "127.0.0.1";
            Settings.Default.healthCheckIntervalWhileHealthy = 1000;
            Settings.Default.processesToKillOnOutage = new List<string> { "calc" };
        }

        [Fact]
        public void ConsoleStart()
        {
            Console.SetIn(new StringReader("a"));
            MainClass.Main(new[] { "--console" });
        }

        [Fact(Skip = "Trying to start service from test shows an interactive messagebox")]
        public void ServiceStart()
        {
            MainClass.Main(new string[0]);
        }
    }
}