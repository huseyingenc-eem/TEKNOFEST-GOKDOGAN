namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Otonom kilitlenme + hedef seçim + kamikaze parametreleri. KTR dokümanındaki
/// multi-criteria scoring ve sliding-window sürelerini tutar.
/// </summary>
public sealed class AutonomyOptions
{
    // --- Hedef seçim skor ağırlıkları (toplamı ~1.0) ---
    public double WeightDistance { get; set; } = 0.4;
    public double WeightAngle { get; set; } = 0.3;
    public double WeightHistory { get; set; } = 0.2;
    public double WeightRisk { get; set; } = 0.1;

    // --- Kilitlenme kuralları (şartname) ---
    /// <summary>Kutu boyutu tolerans yüzdesi (şartname %5; emniyet için %6).</summary>
    public double LockTolerancePercent { get; set; } = 6;

    /// <summary>Kayma penceresi toplam süresi (saniye).</summary>
    public double LockWindowSeconds { get; set; } = 5;

    /// <summary>Bu pencere içinde kilidin sürmesi gereken süre (saniye).</summary>
    public double LockRequiredSeconds { get; set; } = 4;

    // --- Kamikaze parametreleri (TYF taahhüdü) ---
    public double KamikazeApproachAltitudeMeters { get; set; } = 100;
    public double KamikazeDiveAngleDegrees { get; set; } = 45;
    public double KamikazePullUpAltitudeMeters { get; set; } = 30;
    public int KamikazeMaxAttempts { get; set; } = 2;
}
