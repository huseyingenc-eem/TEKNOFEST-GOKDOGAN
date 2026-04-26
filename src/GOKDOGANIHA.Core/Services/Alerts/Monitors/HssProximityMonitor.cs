using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
public sealed class HssProximityMonitor : INotifyPropertyChanged, IDisposable
{
    private readonly FlightState _state;
    private readonly AlertOptions _alertOptions;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private readonly HssPollService _poll;
    private readonly HashSet<int> _alertedIds = new();
    private DateTime? _firstViolationUtc;
    private int _activeViolationCount;
    private TimeSpan _activeViolationDuration;

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

    /// <summary>UI binding: şu an eş zamanlı kaç HSS ihlali aktif (0 = temiz).</summary>
    public int ActiveViolationCount
    {
        get => _activeViolationCount;
        private set { if (_activeViolationCount != value) { _activeViolationCount = value; OnPropertyChanged(); } }
    }

    /// <summary>UI binding: ihlal başladığından beri geçen süre (sıfırsa temiz).</summary>
    public TimeSpan ActiveViolationDuration
    {
        get => _activeViolationDuration;
        private set { if (_activeViolationDuration != value) { _activeViolationDuration = value; OnPropertyChanged(); } }
    }

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

        // İhlal sayısı + süresi UI için canlı güncelle.
        ActiveViolationCount = _alertedIds.Count;
        if (_alertedIds.Count > 0)
        {
            _firstViolationUtc ??= _clock.UtcNow;
            ActiveViolationDuration = _clock.UtcNow - _firstViolationUtc.Value;
        }
        else
        {
            _firstViolationUtc = null;
            ActiveViolationDuration = TimeSpan.Zero;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => _poll.HssUpdated -= OnHssUpdated;
}
