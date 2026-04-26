using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.Core.Services.Failsafe;

/// <summary>
/// GCS haberleşme kaybı + batarya failsafe izleme.
/// - Son telemetri alma zamanını <see cref="RecordHeartbeat"/> ile güncelle.
/// - <see cref="Tick"/> sık aralıklarla çağrılmalı (örn. 1 Hz). Eğer
///   now - lastHeartbeat > GcsTimeoutSeconds ise bir kez alert publish edilir
///   ve <see cref="IFlightCommandSink.Rtl"/> çağrılır.
///
/// Zaman IClock üzerinden; test'te fake-clock ile fast-forward yapılır.
/// </summary>
public sealed class FailsafeMonitor : INotifyPropertyChanged
{
    private readonly FailsafeOptions _options;
    private readonly IAlertPublisher _publisher;
    private readonly IFlightCommandSink _commands;
    private readonly IClock _clock;

    private DateTime? _lastHeartbeat;
    private bool _gcsLostAlerted;
    private bool _isGcsLost;

    public FailsafeMonitor(
        FailsafeOptions options,
        IAlertPublisher publisher,
        IFlightCommandSink commands,
        IClock clock)
    {
        _options = options;
        _publisher = publisher;
        _commands = commands;
        _clock = clock;
    }

    /// <summary>ConnectionOrchestrator / TelemetryPoll her cevap aldığında çağırır.</summary>
    public void RecordHeartbeat()
    {
        _lastHeartbeat = _clock.UtcNow;
        _gcsLostAlerted = false; // recovered
        IsGcsLost = false;
    }

    /// <summary>Tick — GCS timeout'u kontrol et.</summary>
    public void Tick()
    {
        if (_lastHeartbeat is null || _gcsLostAlerted) return;
        var elapsed = (_clock.UtcNow - _lastHeartbeat.Value).TotalSeconds;
        if (elapsed < _options.GcsTimeoutSeconds) return;

        _publisher.Publish(Alert.Create(
            kind: "failsafe-gcs",
            level: AlertLevel.Danger,
            title: "HABERLEŞME KAYBI",
            message: $"Sunucu ile {elapsed:F0} sn temas yok — RTL",
            timeUtc: _clock.UtcNow));

        _commands.Rtl();
        _gcsLostAlerted = true;
        IsGcsLost = true;
    }

    /// <summary>UI binding: GCS heartbeat kaybedildi mi (RTL tetiklendiyse true).</summary>
    public bool IsGcsLost
    {
        get => _isGcsLost;
        private set { if (_isGcsLost != value) { _isGcsLost = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
