using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.Core.Services.Alerts.Monitors;

/// <summary>
/// TelemetryPoll'un her cevabı arası geçen süreyi izler. Son iki TelemetryReceived
/// arasındaki delta <see cref="AlertOptions.CommLatencyThreshold"/>'u aşarsa
/// alert publish eder. PollFailed event'i ayrıca bir hata alert'i tetikler.
/// </summary>
public sealed class CommLatencyMonitor : INotifyPropertyChanged, IDisposable
{
    private readonly AlertOptions _alertOptions;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private readonly TelemetryPollService _poll;
    private DateTime? _lastReceived;
    private bool _latencyAlerted;
    private int _dropoutCount;

    public CommLatencyMonitor(
        AlertOptions alertOptions, IAlertPublisher publisher,
        IClock clock, TelemetryPollService poll)
    {
        _alertOptions = alertOptions;
        _publisher = publisher;
        _clock = clock;
        _poll = poll;
        _poll.TelemetryReceived += OnTelemetry;
        _poll.PollFailed += OnPollFailed;
    }

    private void OnTelemetry(object? sender, TelemetryResponse resp)
    {
        var now = _clock.UtcNow;
        if (_lastReceived.HasValue)
        {
            var delta = (now - _lastReceived.Value).TotalMilliseconds;
            var over = delta > _alertOptions.CommLatencyThreshold;

            if (over && !_latencyAlerted)
            {
                _publisher.Publish(Alert.Create(
                    kind: "comm-latency",
                    level: AlertLevel.Warn,
                    title: "HABERLEŞME GECİKME",
                    message: $"Son paket {delta:0} ms önce (eşik {_alertOptions.CommLatencyThreshold} ms)",
                    timeUtc: now));
                _latencyAlerted = true;
            }
            else if (!over && _latencyAlerted) _latencyAlerted = false;
        }
        _lastReceived = now;
    }

    private void OnPollFailed(object? sender, Exception ex)
    {
        DropoutCount++;
        _publisher.Publish(Alert.Create(
            kind: "comm-fail",
            level: AlertLevel.Danger,
            title: "TELEMETRİ HATASI",
            message: ex.Message,
            timeUtc: _clock.UtcNow));
    }

    /// <summary>UI binding: oturum boyunca kaç PollFailed event'i ateşlendi.</summary>
    public int DropoutCount
    {
        get => _dropoutCount;
        private set { if (_dropoutCount != value) { _dropoutCount = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        _poll.TelemetryReceived -= OnTelemetry;
        _poll.PollFailed -= OnPollFailed;
    }
}
