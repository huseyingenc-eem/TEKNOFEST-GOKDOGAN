using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;

namespace GOKDOGANIHA.Core.Services.Polling;

// Periodically pushes own telemetry to /api/telemetri_gonder and broadcasts the
// server response (sunucusaati + other teams' KonumBilgileri) via TelemetryReceived.
// Spec: ≥1 Hz and ≤2 Hz. We run at ~1.5 Hz (667 ms) for safety margin.
public sealed class TelemetryPollService : IDisposable
{
    private readonly IGameServerClient _client;
    private readonly TimeSpan _interval;
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

    public void Start()
    {
        if (_loop is not null) return;
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunLoop(_cts.Token));
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
    }

    private async Task RunLoop(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);
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

    public void Dispose() => StopAsync().GetAwaiter().GetResult();
}
