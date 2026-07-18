using System;
using System.Collections.Generic;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Alerts.Monitors;

namespace GOKDOGANIHA.Tests.Services;

public class ProximityMonitorTests
{
    private sealed class FakeClock : IClock { public DateTime UtcNow { get; set; } = DateTime.UtcNow; }
    private sealed class CapturePublisher : IAlertPublisher
    {
        public List<Alert> Published { get; } = new();
        public void Publish(Alert alert) => Published.Add(alert);
    }

    [Fact]
    public void BoundaryProximity_fires_when_inside_buffer_and_resets_when_far()
    {
        var opts = new AlertOptions { BoundaryProximityThreshold = 200 };
        var pub = new CapturePublisher();

        // CompetitionBoundary.Corners: Ankara ~Etimesgut, kare ~1km.
        // Merkeze koysak sınırdan uzakta → alert yok.
        var state = new FlightState
        {
            Latitude = CompetitionBoundary.Center.Lat,
            Longitude = CompetitionBoundary.Center.Lng
        };
        var m = new BoundaryProximityMonitor(state, opts, pub, new FakeClock());

        m.Evaluate();
        Assert.Empty(pub.Published);

        // Sınırın çok dışına taşı — kenar mesafesi buffer altına düşer (≈0 m).
        state.Latitude = CompetitionBoundary.Corners[0].Lat + 0.0002; // ~22 m dışarı
        m.Evaluate();

        Assert.Single(pub.Published);
        Assert.Equal("boundary", pub.Published[0].Kind);
    }

    [Fact]
    public void OpponentProximity_fires_once_per_team_below_threshold()
    {
        var opts = new AlertOptions { OpponentProximityThreshold = 500 };
        var pub = new CapturePublisher();
        var state = new FlightState { Latitude = 41.0, Longitude = 29.0 };

        // Monitor poll event'ine subscribe olur ama biz doğrudan Evaluate
        // çağırıyoruz — real TelemetryPollService instantiate edip hiç Start
        // etmiyoruz (dummy client request göndermez, sadece ctor'da saklanır).
        using var dummyClient = new GOKDOGANIHA.Core.Services.Api.GameServerClient(
            new GameServerOptions { BaseUrl = "http://0.0.0.0:0" });
        using var poll = new GOKDOGANIHA.Core.Services.Polling.TelemetryPollService(dummyClient);
        var m = new OpponentProximityMonitor(state, opts, pub, new FakeClock(), poll);

        var others = new[]
        {
            new KonumBilgisi(2, 41.001, 29.0, 100, 0, 0, 0, 20, 100),
            new KonumBilgisi(3, 41.5,   36.0, 100, 0, 0, 0, 20, 100)
        };

        m.Evaluate(others);
        // Sadece 2 numaralı takım yakın (~111 m); 3 uzakta
        Assert.Single(pub.Published);
        Assert.Contains("Takım 2", pub.Published[0].Message);

        // Tekrar aynı listede — hysteresis, re-publish yok
        m.Evaluate(others);
        Assert.Single(pub.Published);
    }

}
