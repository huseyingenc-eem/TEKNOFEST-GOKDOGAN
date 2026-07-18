namespace GOKDOGANIHA.Core.Models;

/// <summary>Uygulamanın kendi İHA durumunu hangi arka uçtan aldığını belirtir.</summary>
public enum FlightDataMode
{
    Live,
    Simulation
}

/// <summary>Kaynak geçişi ve bağlantı sağlığının kullanıcıya gösterilen çalışma durumu.</summary>
public enum FlightBackendStatus
{
    Disconnected,
    Switching,
    ConnectingLive,
    Live,
    StartingSimulation,
    Simulation,
    Faulted
}

public sealed record FlightModeSwitchResult(bool Success, FlightDataMode RequestedMode, string Message);
