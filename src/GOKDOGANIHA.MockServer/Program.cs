using System.Collections.Concurrent;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Telemetry;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MockCompetitionState>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

app.MapGet("/", (MockCompetitionState state) => Results.Ok(new
{
    service = "GOKDOGAN TEKNOFEST Mock Server",
    scenario = state.Scenario,
    serverTimeUtc = DateTime.UtcNow,
    activeTeams = state.ActiveTeamCount,
    endpoints = new[]
    {
        "POST /api/giris",
        "GET /api/sunucusaati",
        "POST /api/telemetri_gonder",
        "POST /api/kilitlenme_bilgisi",
        "POST /api/kamikaze_bilgisi",
        "GET /api/qr_koordinati",
        "GET /api/hss_koordinatlari",
        "GET /api/mock/state",
        "POST /api/mock/scenario/{name}",
        "POST /api/mock/reset"
    }
}));

app.MapPost("/api/giris", (LoginRequest request, MockCompetitionState state, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.KullaniciAdi)
        || string.IsNullOrWhiteSpace(request.Sifre))
    {
        return Results.BadRequest(new { hata = "Kullanıcı adı ve şifre zorunludur." });
    }

    var teamNumber = state.Login(request.KullaniciAdi);
    context.Response.Cookies.Append("gokdogan_mock_session", teamNumber.ToString(), new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict
    });
    return Results.Ok(new LoginResponse(teamNumber));
});

app.MapGet("/api/sunucusaati", (HttpContext context) =>
    TryGetSessionTeam(context, out _)
        ? Results.Ok(MockCompetitionState.Now())
        : Results.Unauthorized());

app.MapPost("/api/telemetri_gonder", async (
    TelemetryPacket packet,
    MockCompetitionState state,
    HttpContext context) =>
{
    if (!TryGetSessionTeam(context, out var sessionTeam))
        return Results.Unauthorized();
    if (packet.TakimNumarasi != sessionTeam)
        return Results.BadRequest(new { hata_kodu = 1, mesaj = "Takım numarası oturumla eşleşmiyor." });

    if (state.Scenario == "high-latency")
        await Task.Delay(900);

    var accepted = state.AcceptTelemetry(packet, out var errorCode, out var error);
    if (!accepted)
        return Results.BadRequest(new { hata_kodu = errorCode, mesaj = error });

    return Results.Ok(new TelemetryResponse(
        MockCompetitionState.Now(),
        state.BuildOpponentPositions(packet.TakimNumarasi)));
});

app.MapPost("/api/kilitlenme_bilgisi", (
    KilitlenmeBilgisi bilgi,
    MockCompetitionState state,
    HttpContext context) =>
{
    if (!TryGetSessionTeam(context, out _)) return Results.Unauthorized();
    state.RecordLock(bilgi);
    return Results.Ok(new { durum = "kaydedildi" });
});

app.MapPost("/api/kamikaze_bilgisi", (
    KamikazeBilgisi bilgi,
    MockCompetitionState state,
    HttpContext context) =>
{
    if (!TryGetSessionTeam(context, out _)) return Results.Unauthorized();
    state.RecordKamikaze(bilgi);
    return Results.Ok(new { durum = "kaydedildi" });
});

app.MapGet("/api/qr_koordinati", (MockCompetitionState state, HttpContext context)
    => TryGetSessionTeam(context, out _)
        ? Results.Ok(state.GetQrCoordinate())
        : Results.Unauthorized());

app.MapGet("/api/hss_koordinatlari", (MockCompetitionState state, HttpContext context)
    => TryGetSessionTeam(context, out _)
        ? Results.Ok(new HssResponse(MockCompetitionState.Now(), state.GetHssZones()))
        : Results.Unauthorized());

app.MapGet("/api/mock/state", (MockCompetitionState state) => Results.Ok(state.Snapshot()));

app.MapPost("/api/mock/scenario/{name}", (string name, MockCompetitionState state) =>
{
    if (!state.TrySetScenario(name, out var validScenarios))
        return Results.BadRequest(new { hata = "Bilinmeyen senaryo", validScenarios });
    return Results.Ok(state.Snapshot());
});

app.MapPost("/api/mock/reset", (MockCompetitionState state) =>
{
    state.Reset();
    return Results.Ok(state.Snapshot());
});

app.Run();

static bool TryGetSessionTeam(HttpContext context, out int teamNumber)
    => int.TryParse(context.Request.Cookies["gokdogan_mock_session"], out teamNumber)
       && teamNumber > 0;

public sealed class MockCompetitionState
{
    private static readonly string[] Scenarios =
    {
        "normal",
        "dense-opponents",
        "hss-active",
        "no-hss",
        "high-latency"
    };

    private readonly ConcurrentDictionary<string, int> _teamsByUsername = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, TelemetryPacket> _latestTelemetry = new();
    private readonly ConcurrentDictionary<int, DateTime> _lastTelemetryUtc = new();
    private readonly ConcurrentQueue<KilitlenmeBilgisi> _locks = new();
    private readonly ConcurrentQueue<KamikazeBilgisi> _kamikaze = new();
    private int _nextTeamNumber;

    public string Scenario { get; private set; } = "normal";
    public int ActiveTeamCount => _latestTelemetry.Count;

    public int Login(string username)
        => _teamsByUsername.GetOrAdd(username, _ => Interlocked.Increment(ref _nextTeamNumber));

    public bool AcceptTelemetry(TelemetryPacket packet, out int errorCode, out string? error)
    {
        var now = DateTime.UtcNow;
        if (_lastTelemetryUtc.TryGetValue(packet.TakimNumarasi, out var previous)
            && now - previous < TimeSpan.FromMilliseconds(500))
        {
            errorCode = 3;
            error = "Telemetri 2 Hz sınırını aşıyor.";
            return false;
        }

        var validation = CompetitionTelemetryValidator.Validate(packet);
        if (!validation.IsValid)
        {
            errorCode = 1;
            error = validation.Message;
            return false;
        }

        _lastTelemetryUtc[packet.TakimNumarasi] = now;
        _latestTelemetry[packet.TakimNumarasi] = packet;
        errorCode = 0;
        error = null;
        return true;
    }

    public IReadOnlyList<KonumBilgisi> BuildOpponentPositions(int requestingTeam)
    {
        var result = _latestTelemetry
            .Where(pair => pair.Key != requestingTeam)
            .Select(pair => ToPosition(pair.Value, 0))
            .ToList();

        var syntheticCount = Scenario == "dense-opponents" ? 8 : 3;
        var t = DateTime.UtcNow.TimeOfDay.TotalSeconds;
        for (var i = 0; i < syntheticCount; i++)
        {
            var angle = t * (0.025 + i * 0.002) + i * Math.PI * 2 / syntheticCount;
            var radius = 0.0035 + i * 0.0007;
            var lat = 39.9208 + Math.Sin(angle) * radius;
            var lon = 32.8541 + Math.Cos(angle) * radius * 1.2;
            result.Add(new KonumBilgisi(
                TakimNumarasi: 101 + i,
                Enlem: lat,
                Boylam: lon,
                Irtifa: 120 + i * 12 + Math.Sin(angle * 2) * 8,
                Dikilme: Math.Sin(angle) * 6,
                Yonelme: (angle * 180 / Math.PI + 90) % 360,
                Yatis: Math.Cos(angle) * 14,
                Hiz: 20 + i,
                ZamanFarkiMs: 40 + i * 35));
        }
        return result;
    }

    public IReadOnlyList<HssKoordinat> GetHssZones()
    {
        if (Scenario == "no-hss") return Array.Empty<HssKoordinat>();
        if (Scenario is not ("hss-active" or "dense-opponents")) return Array.Empty<HssKoordinat>();

        return new[]
        {
            new HssKoordinat(1, 39.9230, 32.8562, 90),
            new HssKoordinat(2, 39.9187, 32.8508, 130),
            new HssKoordinat(3, 39.9179, 32.8585, 70)
        };
    }

    public QrKoordinat GetQrCoordinate() => new(39.9230, 32.8560);
    public void RecordLock(KilitlenmeBilgisi bilgi) => _locks.Enqueue(bilgi);
    public void RecordKamikaze(KamikazeBilgisi bilgi) => _kamikaze.Enqueue(bilgi);

    public bool TrySetScenario(string name, out IReadOnlyList<string> validScenarios)
    {
        validScenarios = Scenarios;
        var normalized = name.Trim().ToLowerInvariant();
        if (!Scenarios.Contains(normalized)) return false;
        Scenario = normalized;
        return true;
    }

    public object Snapshot() => new
    {
        scenario = Scenario,
        activeTeams = ActiveTeamCount,
        users = _teamsByUsername,
        latestTelemetry = _latestTelemetry.Values.OrderBy(x => x.TakimNumarasi),
        lockCount = _locks.Count,
        kamikazeCount = _kamikaze.Count,
        serverTime = Now(),
        validScenarios = Scenarios
    };

    public void Reset()
    {
        Scenario = "normal";
        _teamsByUsername.Clear();
        _latestTelemetry.Clear();
        _lastTelemetryUtc.Clear();
        _locks.Clear();
        _kamikaze.Clear();
        _nextTeamNumber = 0;
    }

    public static ServerTime Now()
    {
        var now = DateTime.UtcNow;
        return new ServerTime(now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
    }

    private static KonumBilgisi ToPosition(TelemetryPacket p, int ageMs) => new(
        p.TakimNumarasi,
        p.Enlem,
        p.Boylam,
        p.Irtifa,
        p.Dikilme,
        p.Yonelme,
        p.Yatis,
        p.Hiz,
        ageMs);
}
