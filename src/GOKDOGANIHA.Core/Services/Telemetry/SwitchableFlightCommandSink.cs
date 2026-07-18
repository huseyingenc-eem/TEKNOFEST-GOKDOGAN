using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Core.Services.Telemetry;

/// <summary>
/// Failsafe ve UI aynı nesneye bağlı kalırken aktif backend'in komut sink'ini atomik
/// olarak değiştirir. Kaynak geçişi sırasında hedef güvenli blocked sink olur.
/// </summary>
public sealed class SwitchableFlightCommandSink : IFlightCommandSink
{
    private IFlightCommandSink _target;

    public SwitchableFlightCommandSink(IFlightCommandSink initialTarget) => _target = initialTarget;

    public void SetTarget(IFlightCommandSink target)
        => Interlocked.Exchange(ref _target, target ?? throw new ArgumentNullException(nameof(target)));

    private IFlightCommandSink Current => Volatile.Read(ref _target);

    public void Arm() => Current.Arm();
    public void Disarm() => Current.Disarm();
    public void SetMode(FlightMode mode) => Current.SetMode(mode);
    public void Rtl() => Current.Rtl();
    public void Land() => Current.Land();
    public void Loiter() => Current.Loiter();
    public void GotoWaypoint(double latitude, double longitude, double altitudeMeters)
        => Current.GotoWaypoint(latitude, longitude, altitudeMeters);
}

/// <summary>Komutların neden uygulanmadığını kullanıcıya bildiren güvenli sink.</summary>
public sealed class BlockedFlightCommandSink : IFlightCommandSink
{
    private readonly Action<string> _onRejected;
    private readonly string _reason;

    public BlockedFlightCommandSink(string reason, Action<string>? onRejected = null)
    {
        _reason = reason;
        _onRejected = onRejected ?? (_ => { });
    }

    public void Arm() => Reject("ARM");
    public void Disarm() => Reject("DISARM");
    public void SetMode(FlightMode mode) => Reject($"MODE {mode}");
    public void Rtl() => Reject("RTL");
    public void Land() => Reject("LAND");
    public void Loiter() => Reject("LOITER");
    public void GotoWaypoint(double latitude, double longitude, double altitudeMeters) => Reject("GOTO");

    private void Reject(string command) => _onRejected($"{command}: {_reason}");
}

/// <summary>
/// Simülasyon komutlarını yalnızca simülasyon state'ine uygular; fiziksel araca hiçbir
/// paket göndermez.
/// </summary>
public sealed class SimulatedFlightCommandSink : IFlightCommandSink
{
    private readonly FlightState _state;
    private readonly Action<string> _onCommand;

    public SimulatedFlightCommandSink(FlightState state, Action<string>? onCommand = null)
    {
        _state = state;
        _onCommand = onCommand ?? (_ => { });
    }

    public void Arm() { _state.IsArmed = true; Emit("ARM"); }
    public void Disarm() { _state.IsArmed = false; Emit("DISARM"); }

    public void SetMode(FlightMode mode)
    {
        _state.Mode = mode;
        _state.IsAutonomous = mode is FlightMode.Auto or FlightMode.Guided;
        Emit($"MODE {mode}");
    }

    public void Rtl() { SetMode(FlightMode.Rtl); Emit("RTL"); }
    public void Land() { SetMode(FlightMode.Land); Emit("LAND"); }
    public void Loiter() { SetMode(FlightMode.Loiter); Emit("LOITER"); }

    public void GotoWaypoint(double latitude, double longitude, double altitudeMeters)
    {
        _state.Latitude = latitude;
        _state.Longitude = longitude;
        _state.Altitude = altitudeMeters;
        _state.Touch("SIMULATION", DateTime.UtcNow);
        Emit($"GOTO {latitude:F5},{longitude:F5} {altitudeMeters:F0}m");
    }

    private void Emit(string message) => _onCommand(message);
}
