using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

public sealed record QrKoordinat(
    [property: JsonPropertyName("qrEnlem")] double Enlem,
    [property: JsonPropertyName("qrBoylam")] double Boylam);
