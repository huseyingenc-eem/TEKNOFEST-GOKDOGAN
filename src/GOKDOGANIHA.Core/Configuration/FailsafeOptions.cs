namespace GOKDOGANIHA.Core.Configuration;

public enum GpsLossBehavior
{
    Loiter,
    Land,
    DeadReckoningThenLand
}

/// <summary>
/// Failsafe davranışları. Şartname zorunlu: GCS haberleşme kaybı 10 sn →
/// otonom iniş. Batarya %20 eşiği yaygın kullanım.
/// </summary>
public sealed class FailsafeOptions
{
    /// <summary>GCS'den paket görülmediğinde kaç saniye sonra alert + komut?</summary>
    public int GcsTimeoutSeconds { get; set; } = 10;

    /// <summary>RTL tetiklenecek batarya yüzdesi.</summary>
    public int BatteryRtlPercent { get; set; } = 20;

    /// <summary>GPS kaybı davranışı.</summary>
    public GpsLossBehavior GpsLoss { get; set; } = GpsLossBehavior.Land;
}
