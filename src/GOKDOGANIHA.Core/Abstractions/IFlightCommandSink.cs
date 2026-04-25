using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Core.Abstractions;

/// <summary>
/// Otopilot komutu ileten katman. Şu an stub; ileride MAVLink adapter
/// tarafından implement edilecek. Failsafe ve orchestrator buraya bağlanır.
/// </summary>
public interface IFlightCommandSink
{
    void Arm();
    void Disarm();
    void SetMode(FlightMode mode);
    void Rtl();
    void Land();
    void Loiter();
    void GotoWaypoint(double latitude, double longitude, double altitudeMeters);
}
