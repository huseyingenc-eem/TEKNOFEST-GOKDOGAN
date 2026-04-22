using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Telemetry;

namespace GOKDOGANIHA.Tests.Services;

public class TelemetryPacketBuilderTests
{
    [Fact]
    public void Build_copies_takim_numarasi_and_FlightState()
    {
        var opts = new GameServerOptions { TakimNumarasi = 7 };
        var state = new FlightState
        {
            Latitude = 41.5,
            Longitude = 36.1,
            Altitude = 120,
            Pitch = 5,
            Heading = 210,
            Roll = -10,
            Speed = 28,
            BatteryPercent = 73,
            IsAutonomous = true,
            IsLocked = true,
            TargetCenterX = 300,
            TargetCenterY = 230,
            TargetWidth = 30,
            TargetHeight = 43
        };
        var builder = new TelemetryPacketBuilder(state, opts);

        var packet = builder.Build(new ServerTime(13, 11, 38, 37, 654));

        Assert.Equal(7, packet.TakimNumarasi);
        Assert.Equal(41.5, packet.Enlem);
        Assert.Equal(28, packet.Hiz);
        Assert.Equal(1, packet.Otonom);
        Assert.Equal(1, packet.Kilitlenme);
        Assert.Equal(300, packet.HedefMerkezX);
        Assert.Equal(654, packet.GpsSaati.Milisaniye);
    }

    [Fact]
    public void Build_encodes_booleans_as_zero_or_one()
    {
        var opts = new GameServerOptions();
        var state = new FlightState { IsAutonomous = false, IsLocked = false };
        var builder = new TelemetryPacketBuilder(state, opts);

        var packet = builder.Build(new ServerTime(1, 0, 0, 0, 0));

        Assert.Equal(0, packet.Otonom);
        Assert.Equal(0, packet.Kilitlenme);
    }
}
