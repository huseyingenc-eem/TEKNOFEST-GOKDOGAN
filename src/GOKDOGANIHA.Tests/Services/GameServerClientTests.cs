using System.Net;
using System.Net.Http;
using System.Text;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;

namespace GOKDOGANIHA.Tests.Services;

public class GameServerClientTests
{
    [Fact]
    public async Task Login_reads_authoritative_team_number()
    {
        using var client = CreateClient(_ => Json(HttpStatusCode.OK, """{"takim_numarasi":42}"""));

        var response = await client.GirisAsync();

        Assert.Equal(42, response.TakimNumarasi);
    }

    [Fact]
    public async Task Http_204_is_treated_as_documented_format_error()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        var ex = await Assert.ThrowsAsync<GameServerProtocolException>(
            () => client.TelemetriGonderAsync(ValidPacket()));

        Assert.Equal(HttpStatusCode.NoContent, ex.StatusCode);
        Assert.Contains("formatı yanlış", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Http_400_preserves_server_error_body()
    {
        using var client = CreateClient(_ => Json(HttpStatusCode.BadRequest, """{"hata_kodu":3}"""));

        var ex = await Assert.ThrowsAsync<GameServerProtocolException>(
            () => client.TelemetriGonderAsync(ValidPacket()));

        Assert.Contains("hata_kodu", ex.Message);
        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public async Task Http_200_with_empty_body_is_not_success_for_data_endpoint()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        });

        await Assert.ThrowsAsync<GameServerProtocolException>(() => client.SunucuSaatiAsync());
    }

    [Fact]
    public async Task Telemetry_response_uses_documented_iha_hizi_field()
    {
        const string body = """
            {
              "sunucusaati":{"gun":17,"saat":12,"dakika":0,"saniye":0,"milisaniye":5},
              "konumBilgileri":[{
                "takim_numarasi":2,
                "iha_enlem":41.51,
                "iha_boylam":36.11,
                "iha_irtifa":44,
                "iha_dikilme":2,
                "iha_yonelme":180,
                "iha_yatis":-3,
                "iha_hizi":41,
                "zaman_farki":248
              }]
            }
            """;
        using var client = CreateClient(_ => Json(HttpStatusCode.OK, body));

        var response = await client.TelemetriGonderAsync(ValidPacket());

        Assert.Single(response.KonumBilgileri);
        Assert.Equal(41, response.KonumBilgileri[0].Hiz);
        Assert.Equal(248, response.KonumBilgileri[0].ZamanFarkiMs);
    }

    private static GameServerClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> response)
    {
        var options = new GameServerOptions
        {
            BaseUrl = "http://127.0.0.1:5000",
            KullaniciAdi = "team",
            Sifre = "secret"
        };
        return new GameServerClient(options, new HttpClient(new DelegateHandler(response)));
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string body) => new(code)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private static TelemetryPacket ValidPacket() => new(
        7, 41.5, 36.1, 100, 0, 180, 0, 25, 80, 1, 0,
        0, 0, 0, 0, new CompetitionTime(12, 0, 0, 0));

    private sealed class DelegateHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _response;
        public DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(_response(request));
    }
}
