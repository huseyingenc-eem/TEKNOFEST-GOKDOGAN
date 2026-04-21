using GOKDOGANIHA.Core.Models.Server;

namespace GOKDOGANIHA.Core.Services.Api;

public interface IGameServerClient
{
    Task<LoginResponse> GirisAsync(CancellationToken ct = default);
    Task<ServerTime> SunucuSaatiAsync(CancellationToken ct = default);
    Task<TelemetryResponse> TelemetriGonderAsync(TelemetryPacket packet, CancellationToken ct = default);
    Task KilitlenmeBilgisiGonderAsync(KilitlenmeBilgisi bilgi, CancellationToken ct = default);
    Task KamikazeBilgisiGonderAsync(KamikazeBilgisi bilgi, CancellationToken ct = default);
    Task<QrKoordinat> QrKoordinatiAsync(CancellationToken ct = default);
    Task<HssResponse> HssKoordinatlariAsync(CancellationToken ct = default);
}
