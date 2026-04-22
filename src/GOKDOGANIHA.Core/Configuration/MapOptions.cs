namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Harita görünümü ve katman tercihleri.
/// </summary>
public sealed class MapOptions
{
    /// <summary>
    /// Tile sağlayıcı adı — "OpenStreetMap" / "GoogleMap" / "GoogleSatellite" /
    /// "StadiaDark" vb. MapPanel bu adı GMapProvider instance'ına çevirir.
    /// </summary>
    public string TileProvider { get; set; } = "GoogleMap";

    public bool ShowGrid { get; set; } = true;
    public bool ShowBoundary { get; set; } = true;
    public bool ShowHssZones { get; set; } = true;
}
