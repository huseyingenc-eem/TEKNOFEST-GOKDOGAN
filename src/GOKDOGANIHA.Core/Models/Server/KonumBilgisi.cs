using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

// Other teams' drone position entry from telemetry response
public sealed record KonumBilgisi(
    [property: JsonPropertyName("takim_numarasi")] int TakimNumarasi,
    [property: JsonPropertyName("iha_enlem")] double Enlem,
    [property: JsonPropertyName("iha_boylam")] double Boylam,
    [property: JsonPropertyName("iha_irtifa")] double Irtifa,
    [property: JsonPropertyName("iha_dikilme")] double Dikilme,
    [property: JsonPropertyName("iha_yonelme")] double Yonelme,
    [property: JsonPropertyName("iha_yatis")] double Yatis,
    [property: JsonPropertyName("iha_hizi")] double Hiz,
    [property: JsonPropertyName("zaman_farki")] int ZamanFarkiMs);

public sealed record TelemetryResponse(
    [property: JsonPropertyName("sunucusaati")] ServerTime SunucuSaati,
    [property: JsonPropertyName("konumBilgileri")] IReadOnlyList<KonumBilgisi> KonumBilgileri);
