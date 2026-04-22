using System;
using System.Collections.Generic;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Services.Alerts.Monitors;

namespace GOKDOGANIHA.Tests.Services;

public class BatteryMonitorTests
{
    private sealed class FakeClock : IClock { public DateTime UtcNow { get; set; } = DateTime.UtcNow; }
    private sealed class CapturePublisher : IAlertPublisher
    {
        public List<Alert> Published { get; } = new();
        public void Publish(Alert alert) => Published.Add(alert);
    }

    [Fact]
    public void Publishes_alert_on_first_drop_below_threshold()
    {
        var state = new FlightState { BatteryVoltage = 24 };
        var opts = new AlertOptions { LowBatteryThreshold = 22 };
        var pub = new CapturePublisher();
        var monitor = new BatteryMonitor(state, opts, pub, new FakeClock());

        state.BatteryVoltage = 21; // drop below

        Assert.Single(pub.Published);
        Assert.Equal("battery", pub.Published[0].Kind);
        Assert.Equal(AlertLevel.Danger, pub.Published[0].Level);
    }

    [Fact]
    public void Does_not_republish_while_still_below_threshold()
    {
        var state = new FlightState { BatteryVoltage = 24 };
        var opts = new AlertOptions { LowBatteryThreshold = 22 };
        var pub = new CapturePublisher();
        _ = new BatteryMonitor(state, opts, pub, new FakeClock());

        state.BatteryVoltage = 21;
        state.BatteryVoltage = 20;
        state.BatteryVoltage = 19;

        Assert.Single(pub.Published);
    }

    [Fact]
    public void Republishes_after_recovery_and_drop()
    {
        var state = new FlightState { BatteryVoltage = 24 };
        var opts = new AlertOptions { LowBatteryThreshold = 22 };
        var pub = new CapturePublisher();
        _ = new BatteryMonitor(state, opts, pub, new FakeClock());

        state.BatteryVoltage = 21; // alert
        state.BatteryVoltage = 25; // recover
        state.BatteryVoltage = 20; // alert again

        Assert.Equal(2, pub.Published.Count);
    }
}
