using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Telemetry;

namespace GOKDOGANIHA.Core.Services.Polling;

// Periodically pushes own telemetry to /api/telemetri_gonder and broadcasts the
// server response (sunucusaati + other teams' KonumBilgileri) via TelemetryReceived.
// Spec: ≥1 Hz and ≤2 Hz. Default 1.5 Hz (667 ms) for safety margin; caller can
// update at runtime via SetInterval.
public sealed class TelemetryPollService : IDisposable
{
    private readonly IGameServerClient _client;
    private TimeSpan _interval;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private TelemetryPacket? _latest;
    private readonly object _packetLock = new();
    private bool _transmissionEnabled = true;
    private double _lastRoundTripMs;
    private string? _lastRejectionMessage;

    public event EventHandler<TelemetryResponse>? TelemetryReceived;
    public event EventHandler<Exception>? PollFailed;
    public event EventHandler<TelemetryValidationResult>? PacketRejected;
    public double LastRoundTripMs => Volatile.Read(ref _lastRoundTripMs);

    public TelemetryPollService(IGameServerClient client, TimeSpan? interval = null)
    {
        _client = client;
        var requested = interval ?? TimeSpan.FromMilliseconds(667);
        _interval = TimeSpan.FromMilliseconds(
            Math.Clamp(requested.TotalMilliseconds, 500, 1000));
    }

    public bool UpdateOwnTelemetry(TelemetryPacket packet)
    {
        var validation = CompetitionTelemetryValidator.Validate(packet);
        lock (_packetLock) _latest = validation.IsValid ? packet : null;
        if (validation.IsValid)
        {
            _lastRejectionMessage = null;
        }
        else if (!string.Equals(_lastRejectionMessage, validation.Message, StringComparison.Ordinal))
        {
            _lastRejectionMessage = validation.Message;
            PacketRejected?.Invoke(this, validation);
        }
        return validation.IsValid;
    }

    public void SetTransmissionEnabled(bool enabled)
    {
        lock (_packetLock)
        {
            _transmissionEnabled = enabled;
            if (!enabled) _latest = null;
        }
    }

    public void ClearOwnTelemetry()
    {
        lock (_packetLock) _latest = null;
    }

    /// <summary>
    /// Tick aralığını çalışma sırasında günceller. Ayarlar sekmesi Hz slider'ı buraya bağlanır.
    /// </summary>
    public void SetInterval(TimeSpan interval)
    {
        // Doküman: en az 1 Hz, en fazla 2 Hz.
        _interval = TimeSpan.FromMilliseconds(
            Math.Clamp(interval.TotalMilliseconds, 500, 1000));
        // PeriodicTimer.Period .NET 8+ ile settable; timer aktifse geri yansır.
        var timer = _timer;
        if (timer is not null) timer.Period = _interval;
    }

    public void Start()
    {
        if (_loop is not null) return;
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(_interval);
        _loop = Task.Run(() => RunLoop(_timer, _cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        _cts.Cancel();
        try { if (_loop is not null) await _loop; }
        catch (OperationCanceledException) { }
        _cts.Dispose();
        _cts = null;
        _loop = null;
        _timer?.Dispose();
        _timer = null;
    }

    private async Task RunLoop(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                TelemetryPacket? packet;
                bool enabled;
                lock (_packetLock)
                {
                    enabled = _transmissionEnabled;
                    packet = _latest;
                }
                if (!enabled) continue;
                if (packet is null) continue;

                try
                {
                    var started = System.Diagnostics.Stopwatch.GetTimestamp();
                    var resp = await _client.TelemetriGonderAsync(packet, ct);
                    var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started);
                    Volatile.Write(ref _lastRoundTripMs, elapsed.TotalMilliseconds);
                    TelemetryReceived?.Invoke(this, resp);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    PollFailed?.Invoke(this, ex);
                }
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
    }

    public void Dispose() => StopAsync().GetAwaiter().GetResult();
}
