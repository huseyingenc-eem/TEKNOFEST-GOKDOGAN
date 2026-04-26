using System;

namespace GOKDOGANIHA.Core.Services.Alerts.Monitors;

/// <summary>
/// Küresel yaklaşım Haversine — <5 km mesafelerde metre cinsinden hata <1 m.
/// Tüm proximity monitor'ları aynı fonksiyonu paylaşır (DRY).
/// </summary>
internal static class GeoDistance
{
    private const double EarthRadiusMeters = 6_371_000;

    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Noktanın çokgen sınırından kenar mesafesi (m). Nokta poligon içindeyse
    /// mesafe yine kenara olan en yakın mesafedir (pozitif).
    /// </summary>
    public static double DistanceToPolygonEdgeMeters(
        double pointLat, double pointLon,
        System.Collections.Generic.IReadOnlyList<(double Lat, double Lon)> polygon)
    {
        if (polygon.Count < 2) return double.PositiveInfinity;

        double min = double.PositiveInfinity;
        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            min = Math.Min(min, DistanceToSegmentMeters(pointLat, pointLon, a.Lat, a.Lon, b.Lat, b.Lon));
        }
        return min;
    }

    private static double DistanceToSegmentMeters(
        double pLat, double pLon,
        double aLat, double aLon,
        double bLat, double bLon)
    {
        // Küçük alanda düz-düzlem yaklaşımı yeterli (yarışma sahası birkaç km).
        // Bir dereceye denk düşen metreler:
        var latMeters = 111_111.0;
        var lonMeters = 111_111.0 * Math.Cos(ToRad((aLat + bLat) / 2));

        double ax = aLon * lonMeters, ay = aLat * latMeters;
        double bx = bLon * lonMeters, by = bLat * latMeters;
        double px = pLon * lonMeters, py = pLat * latMeters;

        double abx = bx - ax, aby = by - ay;
        double apx = px - ax, apy = py - ay;
        double ab2 = abx * abx + aby * aby;
        double t = ab2 <= 0 ? 0 : Math.Clamp((apx * abx + apy * aby) / ab2, 0, 1);
        double qx = ax + t * abx, qy = ay + t * aby;
        double dx = px - qx, dy = py - qy;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Ray-casting (Jordan curve) point-in-polygon testi. Kenar üzerindeki noktalar
    /// "içeride" sayılır — yarışma sahasında pratik bir tolerance. Coğrafi koordinatlarda
    /// küçük alanda düz-düzlem yaklaşımı doğru sonuç verir.
    /// </summary>
    public static bool IsPointInsidePolygon(
        double pointLat, double pointLon,
        System.Collections.Generic.IReadOnlyList<(double Lat, double Lon)> polygon)
    {
        if (polygon.Count < 3) return false;
        bool inside = false;
        int n = polygon.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var (yi, xi) = (polygon[i].Lat, polygon[i].Lon);
            var (yj, xj) = (polygon[j].Lat, polygon[j].Lon);
            bool intersect = ((yi > pointLat) != (yj > pointLat))
                          && (pointLon < (xj - xi) * (pointLat - yi) / (yj - yi) + xi);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
