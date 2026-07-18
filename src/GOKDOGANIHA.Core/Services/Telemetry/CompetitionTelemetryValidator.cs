using GOKDOGANIHA.Core.Models.Server;

namespace GOKDOGANIHA.Core.Services.Telemetry;

/// <summary>
/// Yarışma sunucusuna gitmeden önce telemetri paketini 2026 haberleşme
/// dokümanındaki alan kurallarına göre doğrular. Bir alan hatalıysa dokümana
/// göre paketin tamamı geçersiz sayılır.
/// </summary>
public static class CompetitionTelemetryValidator
{
    public static TelemetryValidationResult Validate(TelemetryPacket packet)
    {
        var errors = new List<string>();

        if (packet.TakimNumarasi <= 0)
            errors.Add("takim_numarasi sıfırdan büyük olmalıdır.");

        RequireFiniteRange(packet.Enlem, -90, 90, "iha_enlem", errors);
        RequireFiniteRange(packet.Boylam, -180, 180, "iha_boylam", errors);
        RequireFinite(packet.Irtifa, "iha_irtifa", errors);
        RequireFiniteRange(packet.Dikilme, -90, 90, "iha_dikilme", errors);
        RequireFiniteRange(packet.Yonelme, 0, 360, "iha_yonelme", errors);
        RequireFiniteRange(packet.Yatis, -90, 90, "iha_yatis", errors);
        RequireFiniteRange(packet.Hiz, 0, double.MaxValue, "iha_hiz", errors);

        if (packet.Batarya is < 0 or > 100)
            errors.Add("iha_batarya 0-100 aralığında olmalıdır.");
        if (packet.Otonom is not (0 or 1))
            errors.Add("iha_otonom yalnızca 0 veya 1 olabilir.");
        if (packet.Kilitlenme is not (0 or 1))
            errors.Add("iha_kilitlenme yalnızca 0 veya 1 olabilir.");

        if (packet.Kilitlenme == 1)
        {
            if (packet.HedefMerkezX <= 0 || packet.HedefMerkezY <= 0)
                errors.Add("Kilitlenmede hedef merkez koordinatları sıfırdan büyük olmalıdır.");
            if (packet.HedefGenislik <= 0 || packet.HedefYukseklik <= 0)
                errors.Add("Kilitlenmede hedef genişliği ve yüksekliği sıfırdan büyük olmalıdır.");
        }

        ValidateTime(packet.GpsSaati, "gps_saati", errors);
        return new TelemetryValidationResult(errors);
    }

    private static void RequireFiniteRange(
        double value,
        double minimum,
        double maximum,
        string field,
        ICollection<string> errors)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
            errors.Add($"{field} {minimum:0.##}-{maximum:0.##} aralığında sonlu bir değer olmalıdır.");
    }

    private static void RequireFinite(double value, string field, ICollection<string> errors)
    {
        if (!double.IsFinite(value))
            errors.Add($"{field} sonlu bir değer olmalıdır.");
    }

    private static void ValidateTime(CompetitionTime time, string field, ICollection<string> errors)
    {
        if (time.Saat is < 0 or > 23
            || time.Dakika is < 0 or > 59
            || time.Saniye is < 0 or > 59
            || time.Milisaniye is < 0 or > 999)
        {
            errors.Add($"{field} geçerli UTC saat alanları içermelidir.");
        }
    }
}

public sealed class TelemetryValidationResult
{
    public TelemetryValidationResult(IReadOnlyList<string> errors) => Errors = errors;

    public IReadOnlyList<string> Errors { get; }
    public bool IsValid => Errors.Count == 0;
    public string Message => string.Join(" ", Errors);
}
