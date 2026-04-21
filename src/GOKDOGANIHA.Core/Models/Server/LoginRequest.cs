using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

public sealed record LoginRequest(
    [property: JsonPropertyName("kadi")] string KullaniciAdi,
    [property: JsonPropertyName("sifre")] string Sifre);

public sealed record LoginResponse(
    [property: JsonPropertyName("takim_numarasi")] int TakimNumarasi);
