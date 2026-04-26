namespace GOKDOGANIHA.Core.Models;

/// <summary>
/// Sinyal karıştırma bölgesi. Şartnamenin "jamming" bölgeleri haritada
/// görselleştirilir; HSS'ten farklı renk/desen ile ayırt edilir.
/// Sunucu endpoint'i belirsizse Settings'ten manuel girilir.
/// </summary>
public sealed record JammingZone(
    int Id,
    double Latitude,
    double Longitude,
    double RadiusMeters);
