using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

/// <summary>
/// Yarışma sunucusuna gönderilen araç GPS ve görev olay zamanı.
/// Resmî örneklerde sunucu saatinden farklı olarak "gun" alanı bulunmaz.
/// </summary>
public sealed record CompetitionTime(
    [property: JsonPropertyName("saat")] int Saat,
    [property: JsonPropertyName("dakika")] int Dakika,
    [property: JsonPropertyName("saniye")] int Saniye,
    [property: JsonPropertyName("milisaniye")] int Milisaniye)
{
    public static CompetitionTime FromUtc(DateTime utc)
        => new(utc.Hour, utc.Minute, utc.Second, utc.Millisecond);
}
