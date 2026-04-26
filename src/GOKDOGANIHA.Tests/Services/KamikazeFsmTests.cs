using System;
using System.Collections.Generic;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Autonomy;

namespace GOKDOGANIHA.Tests.Services;

public class KamikazeFsmTests
{
    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = DateTime.UnixEpoch;
        public void Advance(TimeSpan t) => UtcNow += t;
    }

    private static AutonomyOptions DefaultOptions() => new()
    {
        KamikazeApproachAltitudeMeters = 100,
        KamikazeDiveAngleDegrees = 45,
        KamikazePullUpAltitudeMeters = 30,
        KamikazeMaxAttempts = 2
    };

    /// <summary>Hedef koordinat — 100m kuzeye haversine ile yaklaşım simüle.</summary>
    private const double TargetLat = 41.020;
    private const double TargetLng = 29.010;

    private static FlightState SimState(double alt, double lat = TargetLat, double lng = TargetLng)
        => new() { Altitude = alt, Latitude = lat, Longitude = lng };

    [Fact]
    public void Initial_state_is_Idle()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        Assert.Equal(KamikazePhase.Idle, f.Phase);
        Assert.False(f.IsActive);
    }

    [Fact]
    public void StartMission_transitions_to_Intikal()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        f.StartMission(TargetLat, TargetLng);
        Assert.Equal(KamikazePhase.Intikal, f.Phase);
        Assert.True(f.IsActive);
        Assert.Equal(1, f.AttemptCount);
    }

    [Fact]
    public void Approaching_at_target_altitude_triggers_Dalis()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        f.StartMission(TargetLat, TargetLng);

        // Yaklaşma irtifasında ve hedefe çok yakın
        f.Tick(SimState(alt: 95, TargetLat, TargetLng));
        Assert.Equal(KamikazePhase.Dalis, f.Phase);
    }

    [Fact]
    public void Diving_below_pullup_threshold_triggers_QrAriyor()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        f.StartMission(TargetLat, TargetLng);
        f.Tick(SimState(95)); // Intikal → Dalis
        f.Tick(SimState(40)); // Dalis → QrAriyor (pull-up * 1.5 = 45 altı)
        Assert.Equal(KamikazePhase.QrAriyor, f.Phase);
    }

    [Fact]
    public void ApplyQrRead_in_QrAriyor_advances_to_QrOkundu()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        f.StartMission(TargetLat, TargetLng);
        f.Tick(SimState(95));
        f.Tick(SimState(40));
        Assert.Equal(KamikazePhase.QrAriyor, f.Phase);

        f.ApplyQrRead("MOCK-QR-12345");

        Assert.Equal(KamikazePhase.QrOkundu, f.Phase);
        Assert.Equal("MOCK-QR-12345", f.QrText);
    }

    [Fact]
    public void ApplyQrRead_outside_QrAriyor_is_ignored()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        // Idle iken QR okuma denemesi → no-op
        f.ApplyQrRead("X");
        Assert.Equal(KamikazePhase.Idle, f.Phase);
        Assert.Equal("", f.QrText);
    }

    [Fact]
    public void Successful_pullup_reaches_Tamam_and_fires_event()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        var completed = new List<KamikazeMissionResult>();
        f.MissionCompleted += (_, r) => completed.Add(r);

        f.StartMission(TargetLat, TargetLng);
        f.Tick(SimState(95));     // Dalis
        f.Tick(SimState(40));     // QrAriyor
        f.ApplyQrRead("MOCK");    // QrOkundu
        f.Tick(SimState(60));     // QrOkundu → PasGec (alt > 50)
        f.Tick(SimState(105));    // PasGec → Tamam (alt >= 100)

        Assert.Equal(KamikazePhase.Tamam, f.Phase);
        Assert.Single(completed);
        Assert.True(completed[0].Success);
        Assert.Equal("MOCK", completed[0].QrText);
    }

    [Fact]
    public void Missed_qr_within_max_attempts_resets_to_Intikal()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        f.StartMission(TargetLat, TargetLng);
        f.Tick(SimState(95));
        f.Tick(SimState(40));         // QrAriyor
        // QR okumadan pull-up altına in → first attempt fails, retry
        f.Tick(SimState(20));

        Assert.Equal(KamikazePhase.Intikal, f.Phase);
        Assert.Equal(2, f.AttemptCount);
    }

    [Fact]
    public void Exceeding_max_attempts_results_in_Hata()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        var completed = new List<KamikazeMissionResult>();
        f.MissionCompleted += (_, r) => completed.Add(r);

        f.StartMission(TargetLat, TargetLng);
        // 1. attempt fail
        f.Tick(SimState(95));
        f.Tick(SimState(40));
        f.Tick(SimState(20));         // attempt 2'ye geç
        Assert.Equal(2, f.AttemptCount);

        // 2. attempt fail
        f.Tick(SimState(95));
        f.Tick(SimState(40));
        f.Tick(SimState(20));         // max aşıldı

        Assert.Equal(KamikazePhase.Hata, f.Phase);
        Assert.Single(completed);
        Assert.False(completed[0].Success);
    }

    [Fact]
    public void Abort_during_active_mission_emits_failure_event()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        var completed = new List<KamikazeMissionResult>();
        f.MissionCompleted += (_, r) => completed.Add(r);

        f.StartMission(TargetLat, TargetLng);
        f.Abort("Test iptal");

        Assert.Equal(KamikazePhase.Hata, f.Phase);
        Assert.Single(completed);
        Assert.False(completed[0].Success);
        Assert.Contains("Test iptal", completed[0].Reason);
    }

    [Fact]
    public void Abort_when_idle_is_noop()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        var completed = new List<KamikazeMissionResult>();
        f.MissionCompleted += (_, r) => completed.Add(r);

        f.Abort();

        Assert.Equal(KamikazePhase.Idle, f.Phase);
        Assert.Empty(completed);
    }

    [Fact]
    public void Tick_when_idle_does_not_change_phase()
    {
        var f = new KamikazeFsm(DefaultOptions(), new FakeClock());
        f.Tick(SimState(95));
        Assert.Equal(KamikazePhase.Idle, f.Phase);
    }
}
