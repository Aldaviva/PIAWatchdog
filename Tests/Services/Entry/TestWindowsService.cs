using System;
using System.Collections.Generic;
using System.ServiceProcess;
using PIAWatchdog.Entry;
using PIAWatchdog.Properties;
using Xunit;

namespace Tests.Services.Entry
{
    public class TestWindowsService
    {
        private readonly TestableWindowsService service;

        public TestWindowsService()
        {
            service = new TestableWindowsService();

            Settings.Default.healthCheckPingHost = "127.0.0.1";
            Settings.Default.healthCheckInterval = 5000;
            Settings.Default.processesToKillOnOutage = new List<string> { "calc" };
        }

        [Fact]
        public void StartAndStop()
        {
            service.TestStart(new string[0]);
            service.Stop();
            service.Dispose();
        }
    }

    class TestableWindowsService : WindowsService
    {
        public void TestStart(string[] args)
        {
            OnStart(args);
        }
    }
}