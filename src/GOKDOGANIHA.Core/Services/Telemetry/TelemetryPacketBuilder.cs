using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;

namespace GOKDOGANIHA.Core.Services.Telemetry;

/// <summary>
/// FlightState + GameServerOptions'tan şartname uyumlu <see cref="TelemetryPacket"/>
/// üretir. Stateless; her tick'te çağrılabilir. Pure function — kolay test edilir.
/// </summary>
public sealed class TelemetryPacketBuilder
{
    private readonly FlightState _state;
    private readonly GameServerOptions _serverOptions;

    public TelemetryPacketBuilder(FlightState state, GameServerOptions serverOptions)
    {
        _state = state;
        _serverOptions = serverOptions;
    }

    public TelemetryPacket Build(ServerTime gpsSaati) => new(
        TakimNumarasi: _serverOptions.TakimNumarasi,
        Enlem: _state.Latitude,
        Boylam: _state.Longitude,
        Irtifa: _state.Altitude,
        Dikilme: _state.Pitch,
        Yonelme: _state.Heading,
        Yatis: _state.Roll,
        Hiz: _state.Speed,
        Batarya: _state.BatteryPercent,
        Otonom: _state.IsAutonomous ? 1 : 0,
        Kilitlenme: _state.IsLocked ? 1 : 0,
        HedefMerkezX: _state.TargetCenterX,
        HedefMerkezY: _state.TargetCenterY,
        HedefGenislik: _state.TargetWidth,
        HedefYukseklik: _state.TargetHeight,
        GpsSaati: gpsSaati);
}
