using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;

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

    public event EventHandler<TelemetryResponse>? TelemetryReceived;
    public event EventHandler<Exception>? PollFailed;

    public TelemetryPollService(IGameServerClient client, TimeSpan? interval = null)
    {
        _client = client;
        _interval = interval ?? TimeSpan.FromMilliseconds(667);
    }

    public void UpdateOwnTelemetry(TelemetryPacket packet)
    {
        lock (_packetLock) _latest = packet;
    }

    /// <summary>
    /// Tick aralığını çalışma sırasında günceller. Ayarlar sekmesi Hz slider'ı buraya bağlanır.
    /// </summary>
    public void SetInterval(TimeSpan interval)
    {
        _interval = interval;
        // PeriodicTimer.Period .NET 8+ ile settable; timer aktifse geri yansır.
        var timer = _timer;
        if (timer is not null) timer.Period = interval;
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
                lock (_packetLock) packet = _latest;
                if (packet is null) continue;

                try
                {
                    var resp = await _client.TelemetriGonderAsync(packet, ct);
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
