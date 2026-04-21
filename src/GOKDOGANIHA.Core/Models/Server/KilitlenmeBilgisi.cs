using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

public sealed record KilitlenmeBilgisi(
    [property: JsonPropertyName("kilitlenmeBitisZamani")] ServerTime BitisZamani,
    [property: JsonPropertyName("otonom_kilitlenme")] int OtonomKilitlenme);

public sealed record KamikazeBilgisi(
    [property: JsonPropertyName("kamikazeBaslangicZamani")] ServerTime BaslangicZamani,
    [property: JsonPropertyName("kamikazeBitisZamani")] ServerTime BitisZamani,
    [property: JsonPropertyName("qrMetni")] string QrMetni);
