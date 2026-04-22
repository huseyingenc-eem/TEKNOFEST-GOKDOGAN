namespace GOKDOGANIHA.Core.Abstractions;

/// <summary>
/// Otopilot komutu ileten katman. Şu an stub; ileride MAVLink adapter
/// tarafından implement edilecek. Failsafe ve orchestrator buraya bağlanır.
/// </summary>
public interface IFlightCommandSink
{
    void Rtl();
    void Land();
    void Loiter();
}
