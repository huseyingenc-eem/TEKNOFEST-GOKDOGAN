namespace GOKDOGANIHA.Core.Models;

/// <summary>
/// Mission planner waypoint. Faz 4: yalnızca görselleştirme.
/// Faz 9'da MAVLink MISSION_ITEM_INT mesajları ile İHA'ya gönderim.
/// </summary>
public sealed record Waypoint(
    int Index,
    double Latitude,
    double Longitude,
    double AltitudeMeters,
    string Action = "");
