using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Mavlink;
using GOKDOGANIHA.Core.Services.Telemetry;

namespace GOKDOGANIHA.Tests.Services;

public class TelemetryDataPlacementTests
{
    [Fact]
    public async Task Supported_mavlink_messages_place_each_value_in_the_correct_flight_state_field()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port,
            ExpectedSystemId = 23
        });
        await source.StartAsync();
        using var sender = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        await sender.SendAsync(BuildV1(0, HeartbeatPayload(), 50), endpoint);
        await sender.SendAsync(BuildV1(1, SystemStatusPayload(), 124), endpoint);
        await WaitUntilAsync(() => state.Sequence >= 2);
        Assert.Equal(77, state.BatteryPercent);
        Assert.Equal(24, state.BatteryVoltage);

        await sender.SendAsync(BuildV1(24, GpsRawPayload(), 24), endpoint);
        await sender.SendAsync(BuildV1(30, AttitudePayload(), 39), endpoint);
        await sender.SendAsync(BuildV1(33, GlobalPositionPayload(), 104), endpoint);
        await sender.SendAsync(BuildV1(74, VfrHudPayload(), 20), endpoint);
        await sender.SendAsync(BuildV1(109, RadioStatusPayload(), 185), endpoint);
        await sender.SendAsync(BuildV1(147, BatteryStatusPayload(), 154), endpoint);

        await WaitUntilAsync(() => state.Sequence >= 8);

        Assert.True(source.IsReady);
        Assert.True(state.IsDataValid);
        Assert.Equal("MAVLINK", state.DataSource);
        Assert.True(state.IsArmed);
        Assert.True(state.IsAutonomous);
        Assert.Equal(FlightMode.Auto, state.Mode);
        Assert.Equal(GpsFix.Fix3D, state.GpsFix);
        Assert.Equal(16, state.SatelliteCount);
        Assert.Equal(new DateTime(2026, 7, 18, 10, 11, 12, 345, DateTimeKind.Utc), state.GpsTimeUtc);
        Assert.Equal(41.508775, state.Latitude, 6);
        Assert.Equal(36.118335, state.Longitude, 6);
        Assert.Equal(123.456, state.Altitude, 3);
        Assert.Equal(RadiansToDegrees(-0.10), state.Pitch, 4);
        Assert.Equal(RadiansToDegrees(0.25), state.Roll, 4);
        Assert.Equal(215, state.Heading, 3);
        Assert.Equal(31.5, state.GroundSpeed, 3);
        Assert.Equal(27.5, state.Airspeed, 3);
        Assert.Equal(-2.25, state.VerticalSpeed, 3);
        Assert.Equal(68, state.BatteryPercent);
        Assert.Equal(25, state.BatteryVoltage);
        Assert.Equal(-120 + (200 / 255.0 * 100), state.SignalRssi, 3);
    }

    [Fact]
    public void Flight_state_places_every_value_in_the_documented_outgoing_json_field()
    {
        var vehicleGpsUtc = new DateTime(2026, 7, 18, 11, 22, 33, 444, DateTimeKind.Utc);
        var state = new FlightState
        {
            Latitude = 41.500001,
            Longitude = 36.100002,
            Altitude = 103.3,
            Pitch = -4.4,
            Heading = 205.5,
            Roll = 6.6,
            GroundSpeed = 27.7,
            BatteryPercent = 78,
            IsAutonomous = true,
            IsLocked = true,
            TargetCenterX = 301,
            TargetCenterY = 202,
            TargetWidth = 43,
            TargetHeight = 54
        };
        state.Touch("MAVLINK", vehicleGpsUtc);
        var builder = new TelemetryPacketBuilder(
            state,
            new GameServerOptions { TakimNumarasi = 19 });

        var packet = builder.Build(new ServerTime(18, 1, 2, 3, 4));
        using var json = JsonSerializer.SerializeToDocument(packet);
        var root = json.RootElement;

        Assert.Equal(19, root.GetProperty("takim_numarasi").GetInt32());
        Assert.Equal(41.500001, root.GetProperty("iha_enlem").GetDouble());
        Assert.Equal(36.100002, root.GetProperty("iha_boylam").GetDouble());
        Assert.Equal(103.3, root.GetProperty("iha_irtifa").GetDouble());
        Assert.Equal(-4.4, root.GetProperty("iha_dikilme").GetDouble());
        Assert.Equal(205.5, root.GetProperty("iha_yonelme").GetDouble());
        Assert.Equal(6.6, root.GetProperty("iha_yatis").GetDouble());
        Assert.Equal(27.7, root.GetProperty("iha_hiz").GetDouble());
        Assert.Equal(78, root.GetProperty("iha_batarya").GetInt32());
        Assert.Equal(1, root.GetProperty("iha_otonom").GetInt32());
        Assert.Equal(1, root.GetProperty("iha_kilitlenme").GetInt32());
        Assert.Equal(301, root.GetProperty("hedef_merkez_X").GetInt32());
        Assert.Equal(202, root.GetProperty("hedef_merkez_Y").GetInt32());
        Assert.Equal(43, root.GetProperty("hedef_genislik").GetInt32());
        Assert.Equal(54, root.GetProperty("hedef_yukseklik").GetInt32());

        var gps = root.GetProperty("gps_saati");
        Assert.Equal(11, gps.GetProperty("saat").GetInt32());
        Assert.Equal(22, gps.GetProperty("dakika").GetInt32());
        Assert.Equal(33, gps.GetProperty("saniye").GetInt32());
        Assert.Equal(444, gps.GetProperty("milisaniye").GetInt32());
        Assert.False(gps.TryGetProperty("gun", out _));
        Assert.Equal(16, root.EnumerateObject().Count());
    }

    private static byte[] HeartbeatPayload()
    {
        var payload = new byte[9];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, 10); // ArduPlane AUTO
        payload[4] = 1; // MAV_TYPE_FIXED_WING
        payload[6] = 0x80; // armed
        payload[8] = 3;
        return payload;
    }

    private static byte[] GpsRawPayload()
    {
        var payload = new byte[30];
        var utc = new DateTimeOffset(2026, 7, 18, 10, 11, 12, 345, TimeSpan.Zero);
        BinaryPrimitives.WriteUInt64LittleEndian(
            payload,
            (ulong)utc.ToUnixTimeMilliseconds() * 1_000);
        payload[28] = 3;
        payload[29] = 16;
        return payload;
    }

    private static byte[] SystemStatusPayload()
    {
        var payload = new byte[31];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(14), 24_200);
        payload[30] = 77;
        return payload;
    }

    private static byte[] AttitudePayload()
    {
        var payload = new byte[28];
        WriteSingle(payload, 4, 0.25f);
        WriteSingle(payload, 8, -0.10f);
        WriteSingle(payload, 12, 1.50f);
        return payload;
    }

    private static byte[] GlobalPositionPayload()
    {
        var payload = new byte[28];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4), 415_087_750);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(8), 361_183_350);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(16), 123_456);
        BinaryPrimitives.WriteInt16LittleEndian(payload.AsSpan(20), 1_800);
        BinaryPrimitives.WriteInt16LittleEndian(payload.AsSpan(22), 2_400);
        BinaryPrimitives.WriteInt16LittleEndian(payload.AsSpan(24), -150);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(26), 21_234);
        return payload;
    }

    private static byte[] VfrHudPayload()
    {
        var payload = new byte[20];
        WriteSingle(payload, 0, 27.5f);
        WriteSingle(payload, 4, 31.5f);
        BinaryPrimitives.WriteInt16LittleEndian(payload.AsSpan(8), 215);
        WriteSingle(payload, 12, 987.6f); // MSL: AGL irtifayı ezmemeli
        WriteSingle(payload, 16, -2.25f);
        return payload;
    }

    private static byte[] RadioStatusPayload()
    {
        var payload = new byte[9];
        payload[0] = 200;
        return payload;
    }

    private static byte[] BatteryStatusPayload()
    {
        var payload = new byte[36];
        for (var cell = 0; cell < 6; cell++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(5 + cell * 2), 4_100);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(17), ushort.MaxValue);
        payload[35] = 68;
        return payload;
    }

    private static void WriteSingle(byte[] payload, int offset, float value)
        => BinaryPrimitives.WriteInt32LittleEndian(
            payload.AsSpan(offset),
            BitConverter.SingleToInt32Bits(value));

    private static byte[] BuildV1(byte messageId, byte[] payload, byte crcExtra)
    {
        var frame = new byte[6 + payload.Length + 2];
        frame[0] = 0xFE;
        frame[1] = (byte)payload.Length;
        frame[2] = 1;
        frame[3] = 23;
        frame[4] = 1;
        frame[5] = messageId;
        payload.CopyTo(frame, 6);

        ushort crc = 0xFFFF;
        foreach (var value in frame.AsSpan(1, 5 + payload.Length))
            AccumulateCrc(value, ref crc);
        AccumulateCrc(crcExtra, ref crc);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(6 + payload.Length), crc);
        return frame;
    }

    private static void AccumulateCrc(byte value, ref ushort crc)
    {
        var tmp = (byte)(value ^ (byte)(crc & 0xFF));
        tmp ^= (byte)(tmp << 4);
        crc = (ushort)((crc >> 8) ^ (tmp << 8) ^ (tmp << 3) ^ (tmp >> 4));
    }

    private static int GetFreeUdpPort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(20);
        Assert.True(condition(), "MAVLink test paketleri zamanında işlenmedi.");
    }

    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;
}
