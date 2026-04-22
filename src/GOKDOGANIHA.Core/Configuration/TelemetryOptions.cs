namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Telemetri servisi davranışını kontrol eden ayarlar.
/// Şartname: max 2 Hz. Varsayılan 1 Hz güvenli.
/// </summary>
public sealed class TelemetryOptions
{
    /// <summary>
    /// Telemetri paket gönderim frekansı (Hz). 0.5–2.0 arası.
    /// </summary>
    public double Hz { get; set; } = 1.0;

    /// <summary>
    /// Bağlantı kopunca poll servisi otomatik yeniden başlatsın mı?
    /// </summary>
    public bool AutoReconnect { get; set; } = true;
}
