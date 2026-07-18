using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models.Server;

namespace GOKDOGANIHA.Core.Services.Polling;

/// <summary>
/// TelemetryPoll'dan gelen başarılı paketlerin gerçek Hz'ini ve son hata kodunu
/// canlı ölçer. UI ServerCommPanel'da "1.8 Hz · 200 OK · 23 ms" gibi gösterim için.
///
/// Pencere uzunluğu varsayılan 5 sn — kısa burst değil, sürdürülebilir hız.
/// SRP: yalnızca metrik üretir, alert publish etmez (CommLatencyMonitor o işi yapar).
/// </summary>
public sealed class TelemetryHzMeter : INotifyPropertyChanged, IDisposable
{
    private readonly TelemetryPollService _poll;
    private readonly IClock _clock;
    private readonly TimeSpan _window;
    private readonly object _gate = new();
    private readonly Queue<DateTime> _stamps = new();

    private double _currentHz;
    private string _lastStatus = "—";
    private DateTime? _lastReceivedUtc;
    private double _lastLatencyMs;

    public TelemetryHzMeter(TelemetryPollService poll, IClock clock, TimeSpan? window = null)
    {
        _poll = poll;
        _clock = clock;
        _window = window ?? TimeSpan.FromSeconds(5);
        _poll.TelemetryReceived += OnReceived;
        _poll.PollFailed += OnFailed;
    }

    /// <summary>Son <see cref="_window"/> içindeki paket sayısı / pencere süresi.</summary>
    public double CurrentHz
    {
        get { lock (_gate) return _currentHz; }
        private set { if (_currentHz != value) { _currentHz = value; OnPropertyChanged(); } }
    }

    /// <summary>Son cevabın insan-okur durumu — başarılıda "200 OK", hata olduğunda exception mesajı.</summary>
    public string LastStatus
    {
        get { lock (_gate) return _lastStatus; }
        private set { if (_lastStatus != value) { _lastStatus = value; OnPropertyChanged(); } }
    }

    public DateTime? LastReceivedUtc
    {
        get { lock (_gate) return _lastReceivedUtc; }
        private set { if (_lastReceivedUtc != value) { _lastReceivedUtc = value; OnPropertyChanged(); } }
    }

    /// <summary>Son iki paket arasındaki milisaniye farkı — UI gecikme göstergesi.</summary>
    public double LastLatencyMs
    {
        get { lock (_gate) return _lastLatencyMs; }
        private set { if (Math.Abs(_lastLatencyMs - value) > 0.5) { _lastLatencyMs = value; OnPropertyChanged(); } }
    }

    private void OnReceived(object? sender, TelemetryResponse resp)
    {
        var now = _clock.UtcNow;
        double hz;
        lock (_gate)
        {
            _stamps.Enqueue(now);
            while (_stamps.Count > 0 && now - _stamps.Peek() > _window) _stamps.Dequeue();
            hz = _stamps.Count >= 2
                ? (_stamps.Count - 1) / Math.Max(0.001, (now - _stamps.Peek()).TotalSeconds)
                : 0;
        }
        CurrentHz = Math.Round(hz, 1);
        LastReceivedUtc = now;
        LastLatencyMs = _poll.LastRoundTripMs;
        LastStatus = "200 OK";
    }

    /// <summary>
    /// UI tick'inden çağrılır — telemetri kesilirse pencere zamanaşımıyla
    /// otomatik temizlenir, Hz ölçümü "donmuş" değer göstermez. Caller (UI thread)
    /// bunu 100ms gibi sık tetiklemeli.
    /// </summary>
    public void Refresh()
    {
        var now = _clock.UtcNow;
        double hz;
        lock (_gate)
        {
            while (_stamps.Count > 0 && now - _stamps.Peek() > _window) _stamps.Dequeue();
            hz = _stamps.Count >= 2
                ? (_stamps.Count - 1) / Math.Max(0.001, (now - _stamps.Peek()).TotalSeconds)
                : 0;
        }
        CurrentHz = Math.Round(hz, 1);
    }

    private void OnFailed(object? sender, Exception ex)
    {
        LastStatus = ex.Message.Length > 60 ? ex.Message[..60] + "…" : ex.Message;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        _poll.TelemetryReceived -= OnReceived;
        _poll.PollFailed -= OnFailed;
    }
}
