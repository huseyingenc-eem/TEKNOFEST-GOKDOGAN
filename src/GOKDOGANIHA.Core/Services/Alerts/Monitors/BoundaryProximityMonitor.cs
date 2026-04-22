using System.ComponentModel;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.Core.Services.Alerts.Monitors;

/// <summary>
/// FlightState konumunun <see cref="CompetitionBoundary.Corners"/> sınırına
/// mesafesini takip eder. <see cref="AlertOptions.BoundaryProximityThreshold"/>
/// altına inilince warn publish; sınıra içeriden geri dönülünce hysteresis reset.
/// </summary>
public sealed class BoundaryProximityMonitor : System.IDisposable
{
    private readonly FlightState _state;
    private readonly AlertOptions _alertOptions;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private bool _alerted;

    public BoundaryProximityMonitor(
        FlightState state, AlertOptions alertOptions,
        IAlertPublisher publisher, IClock clock)
    {
        _state = state;
        _alertOptions = alertOptions;
        _publisher = publisher;
        _clock = clock;
        _state.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FlightState.Latitude)
         && e.PropertyName != nameof(FlightState.Longitude)) return;
        Evaluate();
    }

    public void Evaluate()
    {
        var corners = System.Linq.Enumerable.Select(
            CompetitionBoundary.Corners,
            c => (c.Lat, c.Lng));
        var polygon = System.Linq.Enumerable.ToList(corners);
        var distance = GeoDistance.DistanceToPolygonEdgeMeters(
            _state.Latitude, _state.Longitude,
            polygon.ConvertAll(p => (p.Lat, p.Lng)));

        var close = distance < _alertOptions.BoundaryProximityThreshold;
        if (close && !_alerted)
        {
            _publisher.Publish(Alert.Create(
                kind: "boundary",
                level: AlertLevel.Warn,
                title: "SINIR YAKLAŞMA",
                message: $"Uçuş sahası sınırına {distance:0} m (eşik {_alertOptions.BoundaryProximityThreshold:0} m)",
                timeUtc: _clock.UtcNow));
            _alerted = true;
        }
        else if (!close && _alerted) _alerted = false;
    }

    public void Dispose() => _state.PropertyChanged -= OnStateChanged;
}
