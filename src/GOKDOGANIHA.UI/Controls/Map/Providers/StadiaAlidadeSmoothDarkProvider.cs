using System;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace GOKDOGANIHA.UI.Controls.Map.Providers;

// Stadia Maps "Alidade Smooth Dark" tile provider.
//
// Produces a dark tactical basemap with muted terrain shading — the "contour
// line" look we want for the GCS. Stadia's free tier allows non-commercial
// use from localhost without a key; production (competition site) requires
// a free API key from https://client.stadiamaps.com/dashboard/ and the
// ?api_key=... query parameter.
//
// Attribution required on-screen: "© Stadia Maps © OpenStreetMap contributors".
public sealed class StadiaAlidadeSmoothDarkProvider : GMapProvider
{
    // Instance field initializers run BEFORE the base constructor, so Id
    // is already populated when GMapProvider's ctor reads it for registry
    // deduplication. A static-field backed GUID would be Guid.Empty at
    // that moment and collide with any other half-initialized provider.
    public override Guid Id { get; } = new("A7B93E24-0F8D-4C6E-B1A9-7D3F5E2C48B6");
    public override string Name { get; } = "Stadia Alidade Smooth Dark";
    public override PureProjection Projection => MercatorProjection.Instance;

    public static readonly StadiaAlidadeSmoothDarkProvider Instance = new();

    private GMapProvider[]? _overlays;

    // Optional API key. Leave null for localhost dev; set before use in prod.
    public static string? ApiKey { get; set; }

    private StadiaAlidadeSmoothDarkProvider()
    {
        MaxZoom = 20;
        RefererUrl = "https://stadiamaps.com/";
        Copyright = "© Stadia Maps © OpenStreetMap contributors";
    }

    public override GMapProvider[] Overlays => _overlays ??= new GMapProvider[] { this };

    public override PureImage? GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileUrl(pos, zoom);
        return GetTileImageUsingHttp(url);
    }

    private static string MakeTileUrl(GPoint pos, int zoom)
    {
        // @2x = retina / high-DPI tiles (sharper on modern displays).
        string baseUrl = $"https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{zoom}/{pos.X}/{pos.Y}@2x.png";
        return string.IsNullOrEmpty(ApiKey) ? baseUrl : $"{baseUrl}?api_key={ApiKey}";
    }
}
