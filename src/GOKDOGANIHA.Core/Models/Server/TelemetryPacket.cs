using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

// Payload sent to POST /api/telemetri_gonder
public sealed record TelemetryPacket(
    [property: JsonPropertyName("takim_numarasi")] int TakimNumarasi,
    [property: JsonPropertyName("iha_enlem")] double Enlem,
    [property: JsonPropertyName("iha_boylam")] double Boylam,
    [property: JsonPropertyName("iha_irtifa")] double Irtifa,
    [property: JsonPropertyName("iha_dikilme")] double Dikilme,
    [property: JsonPropertyName("iha_yonelme")] double Yonelme,
    [property: JsonPropertyName("iha_yatis")] double Yatis,
    [property: JsonPropertyName("iha_hiz")] double Hiz,
    [property: JsonPropertyName("iha_batarya")] int Batarya,
    [property: JsonPropertyName("iha_otonom")] int Otonom,
    [property: JsonPropertyName("iha_kilitlenme")] int Kilitlenme,
    [property: JsonPropertyName("hedef_merkez_X")] int HedefMerkezX,
    [property: JsonPropertyName("hedef_merkez_Y")] int HedefMerkezY,
    [property: JsonPropertyName("hedef_genislik")] int HedefGenislik,
    [property: JsonPropertyName("hedef_yukseklik")] int HedefYukseklik,
    [property: JsonPropertyName("gps_saati")] ServerTime GpsSaati);
