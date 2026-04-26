using System.ComponentModel;
using System.Runtime.CompilerServices;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.Core.Services.Alerts.Monitors;

/// <summary>
/// FlightState konumunun <see cref="CompetitionBoundary.Corners"/> sınırına
/// mesafesini takip eder. <see cref="AlertOptions.BoundaryProximityThreshold"/>
/// altına inilince warn publish; sınıra içeriden geri dönülünce hysteresis reset.
/// SafetyPanel için <see cref="DistanceToEdgeMeters"/> ve <see cref="IsInside"/>
/// observable property'lerini expose eder.
/// </summary>
public sealed class BoundaryProximityMonitor : INotifyPropertyChanged, System.IDisposable
{
    private readonly FlightState _state;
    private readonly AlertOptions _alertOptions;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private bool _alerted;
    private double _distanceToEdgeMeters;
    private bool _isInside = true;

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

    /// <summary>UI binding: en yakın sınır kenarına mesafe (m). Sürekli güncel.</summary>
    public double DistanceToEdgeMeters
    {
        get => _distanceToEdgeMeters;
        private set { if (_distanceToEdgeMeters != value) { _distanceToEdgeMeters = value; OnPropertyChanged(); } }
    }

    /// <summary>UI binding: nokta poligon içinde mi? (winding number testi).</summary>
    public bool IsInside
    {
        get => _isInside;
        private set { if (_isInside != value) { _isInside = value; OnPropertyChanged(); } }
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

        DistanceToEdgeMeters = distance;
        // Ray-casting ile poligon içi/dışı tespiti — GeoDistance.IsPointInsidePolygon.
        IsInside = GeoDistance.IsPointInsidePolygon(
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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => _state.PropertyChanged -= OnStateChanged;
}
