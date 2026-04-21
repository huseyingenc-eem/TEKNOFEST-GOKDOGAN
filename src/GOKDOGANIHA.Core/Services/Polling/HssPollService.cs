using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;

namespace GOKDOGANIHA.Core.Services.Polling;

// Polls /api/hss_koordinatlari periodically. Returns empty list until referee
// announcement, then populated. Default 2 s cadence (spec doesn't mandate).
public sealed class HssPollService : IDisposable
{
    private readonly IGameServerClient _client;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event EventHandler<HssResponse>? HssUpdated;
    public event EventHandler<Exception>? PollFailed;

    public HssPollService(IGameServerClient client, TimeSpan? interval = null)
    {
        _client = client;
        _interval = interval ?? TimeSpan.FromSeconds(2);
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
            try
            {
                var resp = await _client.HssKoordinatlariAsync(ct);
                HssUpdated?.Invoke(this, resp);
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
