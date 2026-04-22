namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Uyarı eşikleri. İlgili Monitor sınıfları FlightState + bu eşikleri dinleyip
/// AlertBus üzerine Alert publish eder. Eşikleri değiştirmek tüketiciye bir
/// event atmaz — Monitor her frame'de mevcut değeri okur.
/// </summary>
public sealed class AlertOptions
{
    /// <summary>Düşük batarya eşiği (Volt). Altına inilince alert.</summary>
    public int LowBatteryThreshold { get; set; } = 22;

    /// <summary>Rakip İHA yakınlık eşiği (metre). Altında "hasım yaklaştı" alert.</summary>
    public double OpponentProximityThreshold { get; set; } = 500;

    /// <summary>HSS merkezine olan tampon mesafe (metre). Yarıçap + bu altında alert.</summary>
    public double HssProximityThreshold { get; set; } = 50;

    /// <summary>Uçuş sahası sınırına yakınlık eşiği (metre).</summary>
    public double BoundaryProximityThreshold { get; set; } = 100;

    /// <summary>Sunucu cevap gecikme eşiği (ms).</summary>
    public int CommLatencyThreshold { get; set; } = 500;

    /// <summary>Sesli uyarılar açık mı?</summary>
    public bool EnableAudioAlerts { get; set; } = true;

    /// <summary>Kilitlenme bip sesi açık mı?</summary>
    public bool EnableLockBeep { get; set; } = true;
}
