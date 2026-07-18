using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.Tests.Services;

public class TelemetryPollServiceTests
{
    [Fact]
    public void Invalid_packet_is_rejected_before_network_send()
    {
        var client = new FakeClient();
        using var poll = new TelemetryPollService(client);
        var rejectionCount = 0;
        poll.PacketRejected += (_, _) => rejectionCount++;
        var invalid = ValidPacket() with { Yonelme = 361 };

        var first = poll.UpdateOwnTelemetry(invalid);
        var duplicate = poll.UpdateOwnTelemetry(invalid);

        Assert.False(first);
        Assert.False(duplicate);
        Assert.Equal(1, rejectionCount);
        Assert.Equal(0, client.TelemetryCallCount);
    }

    [Fact]
    public void Valid_packet_is_accepted()
    {
        using var poll = new TelemetryPollService(new FakeClient());
        Assert.True(poll.UpdateOwnTelemetry(ValidPacket()));
    }

    private static TelemetryPacket ValidPacket() => new(
        7, 41.5, 36.1, 100, 0, 180, 0, 25, 80, 1, 0,
        0, 0, 0, 0, new CompetitionTime(12, 0, 0, 0));

    private sealed class FakeClient : IGameServerClient
    {
        public int TelemetryCallCount { get; private set; }
        public Task<LoginResponse> GirisAsync(CancellationToken ct = default)
            => Task.FromResult(new LoginResponse(7));
        public Task<ServerTime> SunucuSaatiAsync(CancellationToken ct = default)
            => Task.FromResult(new ServerTime(17, 12, 0, 0, 0));
        public Task<TelemetryResponse> TelemetriGonderAsync(TelemetryPacket packet, CancellationToken ct = default)
        {
            TelemetryCallCount++;
            return Task.FromResult(new TelemetryResponse(
                new ServerTime(17, 12, 0, 0, 0),
                Array.Empty<KonumBilgisi>()));
        }
        public Task KilitlenmeBilgisiGonderAsync(KilitlenmeBilgisi bilgi, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task KamikazeBilgisiGonderAsync(KamikazeBilgisi bilgi, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<QrKoordinat> QrKoordinatiAsync(CancellationToken ct = default)
            => Task.FromResult(new QrKoordinat(41, 36));
        public Task<HssResponse> HssKoordinatlariAsync(CancellationToken ct = default)
            => Task.FromResult(new HssResponse(
                new ServerTime(17, 12, 0, 0, 0),
                Array.Empty<HssKoordinat>()));
    }
}
