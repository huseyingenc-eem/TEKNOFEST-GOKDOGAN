using GOKDOGANIHA.Core.Models.Server;

namespace GOKDOGANIHA.UI.Services;

internal static class SimulationDefaults
{
    /// <summary>
    /// Default Ankara target used only while the simulation backend is active.
    /// Live mode must always wait for the competition server coordinate.
    /// </summary>
    public static QrKoordinat QrTarget { get; } = new(39.9230, 32.8560);
}
