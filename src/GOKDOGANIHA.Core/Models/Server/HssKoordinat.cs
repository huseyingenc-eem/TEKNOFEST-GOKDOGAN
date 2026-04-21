using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

// Active forbidden zone (Hava Savunma Sistemi) — circle on the map
public sealed record HssKoordinat(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("hssEnlem")] double Enlem,
    [property: JsonPropertyName("hssBoylam")] double Boylam,
    [property: JsonPropertyName("hssYaricap")] double YaricapMetre);

public sealed record HssResponse(
    [property: JsonPropertyName("sunucusaati")] ServerTime SunucuSaati,
    [property: JsonPropertyName("hss_koordinat_bilgileri")] IReadOnlyList<HssKoordinat> Koordinatlar);
