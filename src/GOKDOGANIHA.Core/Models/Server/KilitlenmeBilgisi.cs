using System.Text.Json.Serialization;

namespace GOKDOGANIHA.Core.Models.Server;

public sealed record KilitlenmeBilgisi(
    [property: JsonPropertyName("kilitlenmeBitisZamani")] CompetitionTime BitisZamani,
    [property: JsonPropertyName("otonom_kilitlenme")] int OtonomKilitlenme);

public sealed record KamikazeBilgisi(
    [property: JsonPropertyName("kamikazeBaslangicZamani")] CompetitionTime BaslangicZamani,
    [property: JsonPropertyName("kamikazeBitisZamani")] CompetitionTime BitisZamani,
    [property: JsonPropertyName("qrMetni")] string QrMetni);
