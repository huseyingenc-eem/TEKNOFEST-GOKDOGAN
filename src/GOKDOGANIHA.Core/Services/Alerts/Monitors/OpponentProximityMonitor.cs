using System;
using System.Collections.Generic;
using System.Linq;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.Core.Services.Alerts.Monitors;

/// <summary>
/// TelemetryPoll cevabındaki rakip İHA listesini dinler. Kendi konumumuz
/// (<see cref="FlightState"/>) ile her rakip arasındaki mesafeyi hesaplar;
/// <see cref="AlertOptions.OpponentProximityThreshold"/> altına inilince
/// takım_numarasına göre hysteresis (her rakip ayrı alert).
/// </summary>
public sealed class OpponentProximityMonitor : IDisposable
{
    private readonly FlightState _state;
    private readonly AlertOptions _alertOptions;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private readonly TelemetryPollService _poll;
    private readonly HashSet<int> _alertedTeams = new();

    public OpponentProximityMonitor(
        FlightState state, AlertOptions alertOptions,
        IAlertPublisher publisher, IClock clock,
        TelemetryPollService poll)
    {
        _state = state;
        _alertOptions = alertOptions;
        _publisher = publisher;
        _clock = clock;
        _poll = poll;
        _poll.TelemetryReceived += OnTelemetry;
    }

    private void OnTelemetry(object? sender, TelemetryResponse resp) => Evaluate(resp.KonumBilgileri);

    public void Evaluate(IReadOnlyList<KonumBilgisi> others)
    {
        var seen = new HashSet<int>();
        foreach (var other in others)
        {
            seen.Add(other.TakimNumarasi);
            var distance = GeoDistance.HaversineMeters(
                _state.Latitude, _state.Longitude, other.Enlem, other.Boylam);
            var close = distance < _alertOptions.OpponentProximityThreshold;

            if (close && !_alertedTeams.Contains(other.TakimNumarasi))
            {
                _publisher.Publish(Alert.Create(
                    kind: "opponent",
                    level: AlertLevel.Warn,
                    title: "RAKİP YAKLAŞTI",
                    message: $"Takım {other.TakimNumarasi} — {distance:0} m (eşik {_alertOptions.OpponentProximityThreshold:0} m)",
                    timeUtc: _clock.UtcNow));
                _alertedTeams.Add(other.TakimNumarasi);
            }
            else if (!close) _alertedTeams.Remove(other.TakimNumarasi);
        }

        // Raporda olmayan takımlar için alert durumunu temizle
        _alertedTeams.RemoveWhere(t => !seen.Contains(t));
    }

    public void Dispose() => _poll.TelemetryReceived -= OnTelemetry;
}
