namespace GOKDOGANIHA.Core.Models;

/// <summary>
/// ArduPilot uçuş modları. MAVLink HEARTBEAT.custom_mode'dan map edilir.
/// </summary>
public enum FlightMode
{
    Manual,
    Stabilize,
    FlyByWireA,
    FlyByWireB,
    Auto,
    Guided,
    Loiter,
    Rtl,
    Circle,
    Land,
    Takeoff,
    Unknown
}
