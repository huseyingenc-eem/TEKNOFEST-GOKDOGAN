namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Uçuş sahası (geofence) ve RTL parametreleri. Şartname zorunlulukları.
/// </summary>
public sealed class GeofenceOptions
{
    /// <summary>Maksimum izin verilen irtifa (metre, relative AGL).</summary>
    public double MaxAltitudeMeters { get; set; } = 300;

    /// <summary>Home'dan izin verilen maksimum mesafe (metre).</summary>
    public double MaxDistanceMeters { get; set; } = 2000;

    /// <summary>RTL tetiklenince dönülecek irtifa (metre, güvenli).</summary>
    public double RtlAltitudeMeters { get; set; } = 120;

    /// <summary>Sınıra yaklaşırken uyarı verilen tampon mesafe (metre).</summary>
    public double BufferMeters { get; set; } = 100;
}
