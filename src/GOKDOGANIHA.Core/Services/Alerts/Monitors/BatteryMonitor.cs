using System;
using System.ComponentModel;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.Core.Services.Alerts.Monitors;

/// <summary>
/// FlightState.BatteryVoltage'u izler; <see cref="AlertOptions.LowBatteryThreshold"/>
/// altına düşünce bir kez alert publish eder (hysteresis — eşik üstüne çıkana kadar
/// tekrar tetiklemez).
/// </summary>
public sealed class BatteryMonitor : IDisposable
{
    private readonly FlightState _state;
    private readonly AlertOptions _alertOptions;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private bool _alerted;

    public BatteryMonitor(
        FlightState state,
        AlertOptions alertOptions,
        IAlertPublisher publisher,
        IClock clock)
    {
        _state = state;
        _alertOptions = alertOptions;
        _publisher = publisher;
        _clock = clock;
        _state.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FlightState.BatteryVoltage)) return;
        Evaluate();
    }

    /// <summary>Test edilebilir manuel değerlendirme.</summary>
    public void Evaluate()
    {
        var below = _state.BatteryVoltage < _alertOptions.LowBatteryThreshold;

        if (below && !_alerted)
        {
            _publisher.Publish(Alert.Create(
                kind: "battery",
                level: AlertLevel.Danger,
                title: "BATARYA KRİTİK",
                message: $"Batarya {_state.BatteryVoltage} V (eşik {_alertOptions.LowBatteryThreshold} V) — RTL önerilir",
                timeUtc: _clock.UtcNow));
            _alerted = true;
        }
        else if (!below && _alerted)
        {
            // Hysteresis reset — tekrar tetiklenebilir
            _alerted = false;
        }
    }

    public void Dispose() => _state.PropertyChanged -= OnStateChanged;
}
