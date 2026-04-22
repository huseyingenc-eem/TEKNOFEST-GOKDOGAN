using System;
using System.Collections.Generic;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Services.Failsafe;

namespace GOKDOGANIHA.Tests.Services;

public class FailsafeMonitorTests
{
    private sealed class FakeClock : IClock { public DateTime UtcNow { get; set; } = DateTime.UnixEpoch; }
    private sealed class CapturePublisher : IAlertPublisher
    {
        public List<Alert> Published { get; } = new();
        public void Publish(Alert alert) => Published.Add(alert);
    }
    private sealed class FakeCommands : IFlightCommandSink
    {
        public int RtlCount; public int LandCount; public int LoiterCount;
        public void Rtl() => RtlCount++;
        public void Land() => LandCount++;
        public void Loiter() => LoiterCount++;
    }

    [Fact]
    public void Does_not_fire_before_timeout_elapses()
    {
        var clock = new FakeClock { UtcNow = DateTime.UnixEpoch };
        var pub = new CapturePublisher();
        var cmd = new FakeCommands();
        var opts = new FailsafeOptions { GcsTimeoutSeconds = 10 };
        var m = new FailsafeMonitor(opts, pub, cmd, clock);

        m.RecordHeartbeat();
        clock.UtcNow = DateTime.UnixEpoch.AddSeconds(5);
        m.Tick();

        Assert.Empty(pub.Published);
        Assert.Equal(0, cmd.RtlCount);
    }

    [Fact]
    public void Fires_once_after_timeout_and_calls_Rtl()
    {
        var clock = new FakeClock { UtcNow = DateTime.UnixEpoch };
        var pub = new CapturePublisher();
        var cmd = new FakeCommands();
        var opts = new FailsafeOptions { GcsTimeoutSeconds = 10 };
        var m = new FailsafeMonitor(opts, pub, cmd, clock);

        m.RecordHeartbeat();
        clock.UtcNow = DateTime.UnixEpoch.AddSeconds(11);
        m.Tick();
        m.Tick(); // second tick while still lost — no duplicate
        m.Tick();

        Assert.Single(pub.Published);
        Assert.Equal(AlertLevel.Danger, pub.Published[0].Level);
        Assert.Equal("failsafe-gcs", pub.Published[0].Kind);
        Assert.Equal(1, cmd.RtlCount);
    }

    [Fact]
    public void RecordHeartbeat_resets_alert_state()
    {
        var clock = new FakeClock { UtcNow = DateTime.UnixEpoch };
        var pub = new CapturePublisher();
        var cmd = new FakeCommands();
        var opts = new FailsafeOptions { GcsTimeoutSeconds = 5 };
        var m = new FailsafeMonitor(opts, pub, cmd, clock);

        m.RecordHeartbeat();
        clock.UtcNow = DateTime.UnixEpoch.AddSeconds(10);
        m.Tick(); // fire

        // Recovered
        m.RecordHeartbeat();
        clock.UtcNow = DateTime.UnixEpoch.AddSeconds(20);
        m.Tick(); // should fire again

        Assert.Equal(2, pub.Published.Count);
        Assert.Equal(2, cmd.RtlCount);
    }
}
