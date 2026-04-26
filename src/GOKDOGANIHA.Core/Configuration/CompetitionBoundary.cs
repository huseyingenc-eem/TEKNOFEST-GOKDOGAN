namespace GOKDOGANIHA.Core.Configuration;

// Yarışma alanı sınır poligonu. Sunucu API'sinde sınır endpoint'i yok;
// TYF hakem duyurusu ile yayınlanan sabit koordinatlar. Placeholder değerler
// yarışma günü gerçek koordinatlarla güncellenmelidir.
public static class CompetitionBoundary
{
    public readonly record struct LatLng(double Lat, double Lng);

    // Placeholder: Ankara ~Etimesgut civarı, ileride TYF koordinatlarıyla değişecek
    public static readonly LatLng Center = new(39.9208, 32.8541);

    // Uçuş izin sahası yarıçapı (metre). Kullanıcı isteğiyle biraz büyütüldü.
    public const double RadiusMeters = 1600;

    public static readonly IReadOnlyList<LatLng> Corners = new[]
    {
        new LatLng(39.9250, 32.8470),
        new LatLng(39.9250, 32.8610),
        new LatLng(39.9165, 32.8610),
        new LatLng(39.9165, 32.8470)
    };
}
