using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
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
        // No BaseAddress: each request resolves the URL from the CURRENT
        // options.BaseUrl, so runtime edits in the SUNUCU settings tab take
        // effect on the next call without reconstructing the client.
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    private Uri Url(string path) => new(new Uri(_options.BaseUrl), path);

    public async Task<LoginResponse> GirisAsync(CancellationToken ct = default)
    {
        var req = new LoginRequest(_options.KullaniciAdi, _options.Sifre);
        using var resp = await _http.PostAsJsonAsync(Url("/api/giris"), req, _json, ct);
        var login = await ReadRequiredJsonAsync<LoginResponse>(resp, "/api/giris", ct);
        // Authoritative team number — server decides, we mirror into options so
        // every subsequent telemetry packet carries the correct takim_numarasi.
        _options.TakimNumarasi = login.TakimNumarasi;
        return login;
    }

    public async Task<ServerTime> SunucuSaatiAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(Url("/api/sunucusaati"), ct);
        return await ReadRequiredJsonAsync<ServerTime>(resp, "/api/sunucusaati", ct);
    }

    public async Task<TelemetryResponse> TelemetriGonderAsync(TelemetryPacket packet, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync(Url("/api/telemetri_gonder"), packet, _json, ct);
        return await ReadRequiredJsonAsync<TelemetryResponse>(resp, "/api/telemetri_gonder", ct);
    }

    public async Task KilitlenmeBilgisiGonderAsync(KilitlenmeBilgisi bilgi, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync(Url("/api/kilitlenme_bilgisi"), bilgi, _json, ct);
        await EnsureOkAsync(resp, "/api/kilitlenme_bilgisi", ct);
    }

    public async Task KamikazeBilgisiGonderAsync(KamikazeBilgisi bilgi, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync(Url("/api/kamikaze_bilgisi"), bilgi, _json, ct);
        await EnsureOkAsync(resp, "/api/kamikaze_bilgisi", ct);
    }

    public async Task<QrKoordinat> QrKoordinatiAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(Url("/api/qr_koordinati"), ct);
        return await ReadRequiredJsonAsync<QrKoordinat>(resp, "/api/qr_koordinati", ct);
    }

    public async Task<HssResponse> HssKoordinatlariAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(Url("/api/hss_koordinatlari"), ct);
        return await ReadRequiredJsonAsync<HssResponse>(resp, "/api/hss_koordinatlari", ct);
    }

    private async Task<T> ReadRequiredJsonAsync<T>(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken ct)
    {
        await EnsureOkAsync(response, endpoint, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        try
        {
            return JsonSerializer.Deserialize<T>(body, _json)
                ?? throw new JsonException("Yanıt gövdesi boş.");
        }
        catch (JsonException ex)
        {
            throw new GameServerProtocolException(response.StatusCode, endpoint,
                $"Geçersiz JSON: {ex.Message}. Gövde: {body}");
        }
    }

    private static async Task EnsureOkAsync(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.OK) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new GameServerProtocolException(response.StatusCode, endpoint, body);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _http.Dispose();
        _disposed = true;
    }
}
