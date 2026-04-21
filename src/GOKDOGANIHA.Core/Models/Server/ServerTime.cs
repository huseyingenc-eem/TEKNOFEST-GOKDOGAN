using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

public sealed record ServerTime(
    [property: JsonPropertyName("gun")] int Gun,
    [property: JsonPropertyName("saat")] int Saat,
    [property: JsonPropertyName("dakika")] int Dakika,
    [property: JsonPropertyName("saniye")] int Saniye,
    [property: JsonPropertyName("milisaniye")] int Milisaniye);
