using System;
using System.Collections.Generic;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.Core.Services.Alerts.Monitors;

/// <summary>
/// HssPoll cevabındaki aktif HSS zonlarını dinler. Kendi konumumuzun her HSS
/// merkezine mesafesi (YariCap + <see cref="AlertOptions.HssProximityThreshold"/>)
/// altına inerse danger alert publish eder. Her HSS id ayrı hysteresis.
/// </summary>
public sealed class HssProximityMonitor : IDisposable
{
    private readonly FlightState _state;
    private readonly AlertOptions _alertOptions;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private readonly HssPollService _poll;
    private readonly HashSet<int> _alertedIds = new();

    public HssProximityMonitor(
        FlightState state, AlertOptions alertOptions,
        IAlertPublisher publisher, IClock clock,
        HssPollService poll)
    {
        _state = state;
        _alertOptions = alertOptions;
        _publisher = publisher;
        _clock = clock;
        _poll = poll;
        _poll.HssUpdated += OnHssUpdated;
    }

    private void OnHssUpdated(object? sender, HssResponse resp) => Evaluate(resp.Koordinatlar);

    public void Evaluate(IReadOnlyList<HssKoordinat> zones)
    {
        var seen = new HashSet<int>();
        foreach (var z in zones)
        {
            seen.Add(z.Id);
            var distance = GeoDistance.HaversineMeters(
                _state.Latitude, _state.Longitude, z.Enlem, z.Boylam);
            var threshold = z.YaricapMetre + _alertOptions.HssProximityThreshold;
            var close = distance < threshold;

            if (close && !_alertedIds.Contains(z.Id))
            {
                _publisher.Publish(Alert.Create(
                    kind: "hss",
                    level: AlertLevel.Danger,
                    title: "HSS YAKLAŞMA",
                    message: $"HSS-{z.Id:00} merkezine {distance:0} m (yarıçap {z.YaricapMetre:0} m + tampon {_alertOptions.HssProximityThreshold:0} m)",
                    timeUtc: _clock.UtcNow));
                _alertedIds.Add(z.Id);
            }
            else if (!close) _alertedIds.Remove(z.Id);
        }

        _alertedIds.RemoveWhere(id => !seen.Contains(id));
    }

    public void Dispose() => _poll.HssUpdated -= OnHssUpdated;
}
