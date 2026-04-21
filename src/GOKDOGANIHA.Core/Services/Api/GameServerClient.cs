using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Server;

namespace GOKDOGANIHA.Core.Services.Api;

public sealed class GameServerClient : IGameServerClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly GameServerOptions _options;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    private bool _disposed;

    public GameServerClient(GameServerOptions options, HttpClient? http = null)
    {
        _options = options;
        _http = http ?? new HttpClient { BaseAddress = new Uri(options.BaseUrl), Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<LoginResponse> GirisAsync(CancellationToken ct = default)
    {
        var req = new LoginRequest(_options.KullaniciAdi, _options.Sifre);
        var resp = await _http.PostAsJsonAsync("/api/giris", req, _json, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<LoginResponse>(_json, ct))!;
    }

    public async Task<ServerTime> SunucuSaatiAsync(CancellationToken ct = default)
        => (await _http.GetFromJsonAsync<ServerTime>("/api/sunucusaati", _json, ct))!;

    public async Task<TelemetryResponse> TelemetriGonderAsync(TelemetryPacket packet, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/telemetri_gonder", packet, _json, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TelemetryResponse>(_json, ct))!;
    }

    public async Task KilitlenmeBilgisiGonderAsync(KilitlenmeBilgisi bilgi, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/kilitlenme_bilgisi", bilgi, _json, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task KamikazeBilgisiGonderAsync(KamikazeBilgisi bilgi, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/kamikaze_bilgisi", bilgi, _json, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<QrKoordinat> QrKoordinatiAsync(CancellationToken ct = default)
        => (await _http.GetFromJsonAsync<QrKoordinat>("/api/qr_koordinati", _json, ct))!;

    public async Task<HssResponse> HssKoordinatlariAsync(CancellationToken ct = default)
        => (await _http.GetFromJsonAsync<HssResponse>("/api/hss_koordinatlari", _json, ct))!;

    public void Dispose()
    {
        if (_disposed) return;
        _http.Dispose();
        _disposed = true;
    }
}
