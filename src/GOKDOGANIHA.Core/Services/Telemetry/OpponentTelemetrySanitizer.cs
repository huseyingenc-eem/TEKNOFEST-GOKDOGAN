using GOKDOGANIHA.Core.Models.Server;

namespace GOKDOGANIHA.Core.Services.Telemetry;

/// <summary>
/// Yarışma sunucusundan gelen rakip telemetrisini UI ve otonomi tüketicilerine
/// verilmeden önce alan aralıkları, sonluluk, gecikme ve tekrarlar bakımından temizler.
/// Aynı takım için en güncel (zaman_farki en küçük) kayıt korunur.
/// </summary>
public static class OpponentTelemetrySanitizer
{
    public const int DefaultMaximumAgeMs = 5_000;

    public static IReadOnlyList<KonumBilgisi> Clean(
        IEnumerable<KonumBilgisi>? positions,
        int maximumAgeMs = DefaultMaximumAgeMs)
    {
        if (positions is null || maximumAgeMs < 0)
            return Array.Empty<KonumBilgisi>();

        return positions
            .Where(position => IsUsable(position, maximumAgeMs))
            .GroupBy(position => position.TakimNumarasi)
            .Select(group => group.OrderBy(position => position.ZamanFarkiMs).First())
            .OrderBy(position => position.TakimNumarasi)
            .ToArray();
    }

    public static bool IsUsable(
        KonumBilgisi position,
        int maximumAgeMs = DefaultMaximumAgeMs)
        => maximumAgeMs >= 0
           && position.TakimNumarasi > 0
           && double.IsFinite(position.Enlem)
           && double.IsFinite(position.Boylam)
           && double.IsFinite(position.Irtifa)
           && double.IsFinite(position.Dikilme)
           && double.IsFinite(position.Yonelme)
           && double.IsFinite(position.Yatis)
           && double.IsFinite(position.Hiz)
           && position.Enlem is >= -90 and <= 90
           && position.Boylam is >= -180 and <= 180
           && position.Dikilme is >= -90 and <= 90
           && position.Yonelme is >= 0 and <= 360
           && position.Yatis is >= -90 and <= 90
           && position.Hiz >= 0
           && position.ZamanFarkiMs is >= 0
           && position.ZamanFarkiMs <= maximumAgeMs;
}
