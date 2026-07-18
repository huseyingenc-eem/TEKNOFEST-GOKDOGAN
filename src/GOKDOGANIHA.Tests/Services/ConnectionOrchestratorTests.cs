using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Services.Session;

namespace GOKDOGANIHA.Tests.Services;

public class ConnectionOrchestratorTests
{
    [Fact]
    public async Task Successful_login_and_manual_disconnect_update_public_connection_state()
    {
        var client = new FakeClient();
        using var telemetry = new TelemetryPollService(client);
        using var hss = new HssPollService(client);
        var orchestrator = new ConnectionOrchestrator(
            client,
            telemetry,
            hss,
            new NullAlerts(),
            new FixedClock(),
            new TelemetryOptions());
        var states = new List<bool>();
        orchestrator.ConnectionStateChanged += (_, connected) => states.Add(connected);

        Assert.True(await orchestrator.ConnectAsync());
        Assert.True(orchestrator.IsConnected);

        await orchestrator.DisconnectAsync();
        Assert.False(orchestrator.IsConnected);
        Assert.Equal(new[] { true, false }, states);
    }

    private sealed class FakeClient : IGameServerClient
    {
        public Task<LoginResponse> GirisAsync(CancellationToken ct = default)
            => Task.FromResult(new LoginResponse(7));
        public Task<ServerTime> SunucuSaatiAsync(CancellationToken ct = default)
            => Task.FromResult(new ServerTime(18, 12, 0, 0, 0));
        public Task<TelemetryResponse> TelemetriGonderAsync(TelemetryPacket packet, CancellationToken ct = default)
            => Task.FromResult(new TelemetryResponse(
                new ServerTime(18, 12, 0, 0, 0), Array.Empty<KonumBilgisi>()));
        public Task KilitlenmeBilgisiGonderAsync(KilitlenmeBilgisi bilgi, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task KamikazeBilgisiGonderAsync(KamikazeBilgisi bilgi, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<QrKoordinat> QrKoordinatiAsync(CancellationToken ct = default)
            => Task.FromResult(new QrKoordinat(41, 36));
        public Task<HssResponse> HssKoordinatlariAsync(CancellationToken ct = default)
            => Task.FromResult(new HssResponse(
                new ServerTime(18, 12, 0, 0, 0), Array.Empty<HssKoordinat>()));
    }

    private sealed class NullAlerts : IAlertPublisher
    {
        public void Publish(Alert alert) { }
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    }
}
