using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Mavlink;

namespace GOKDOGANIHA.Tests.Services;

public class MavlinkFlightStateSourceTests
{
    [Fact]
    public async Task Valid_heartbeat_gps_and_position_populate_live_state()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port,
            ExpectedSystemId = 1
        });
        await source.StartAsync();
        using var sender = new UdpClient();

        await sender.SendAsync(BuildV1(0, HeartbeatPayload(), 50), new IPEndPoint(IPAddress.Loopback, port));
        await sender.SendAsync(BuildV1(24, GpsRawPayload(), 24), new IPEndPoint(IPAddress.Loopback, port));
        await sender.SendAsync(BuildV1(33, GlobalPositionPayload(), 104), new IPEndPoint(IPAddress.Loopback, port));

        await WaitUntilAsync(() => source.IsReady && Math.Abs(state.Latitude - 41.508775) < 0.000001);

        Assert.True(source.IsReady);
        Assert.True(state.IsDataValid);
        Assert.Equal("MAVLINK", state.DataSource);
        Assert.Equal(GpsFix.Fix3D, state.GpsFix);
        Assert.Equal(14, state.SatelliteCount);
        Assert.Equal(0.9, state.GpsHdop);
        Assert.True(state.GpsTimeUtc.HasValue);
        Assert.Equal(41.508775, state.Latitude, 6);
        Assert.Equal(36.118335, state.Longitude, 6);
        Assert.Equal(123.4, state.Altitude, 1);
    }

    [Fact]
    public async Task Invalid_checksum_is_ignored()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port
        });
        await source.StartAsync();
        using var sender = new UdpClient();
        var frame = BuildV1(0, HeartbeatPayload(), 50);
        frame[^1] ^= 0xFF;

        await sender.SendAsync(frame, new IPEndPoint(IPAddress.Loopback, port));
        await Task.Delay(150);

        Assert.False(source.IsReady);
        Assert.False(state.IsDataValid);
    }

    [Fact]
    public async Task Auto_system_id_locks_to_first_valid_heartbeat()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port,
            ExpectedSystemId = 0
        });
        await source.StartAsync();
        using var sender = new UdpClient();

        await sender.SendAsync(BuildV1(0, HeartbeatPayload(), 50, systemId: 7),
            new IPEndPoint(IPAddress.Loopback, port));
        await sender.SendAsync(BuildV1(33, GlobalPositionPayload(), 104, systemId: 9),
            new IPEndPoint(IPAddress.Loopback, port));
        await Task.Delay(100);
        Assert.Equal(0, state.Latitude);

        await sender.SendAsync(BuildV1(33, GlobalPositionPayload(), 104, systemId: 7),
            new IPEndPoint(IPAddress.Loopback, port));
        await WaitUntilAsync(() => Math.Abs(state.Latitude - 41.508775) < 0.000001);
    }

    [Fact]
    public async Task Split_frames_are_reassembled_and_vfr_msl_does_not_replace_relative_altitude()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port,
            ExpectedSystemId = 1
        });
        await source.StartAsync();
        using var sender = new UdpClient();
        var heartbeat = BuildV1(0, HeartbeatPayload(), 50);

        await sender.SendAsync(heartbeat[..4], new IPEndPoint(IPAddress.Loopback, port));
        await sender.SendAsync(heartbeat[4..], new IPEndPoint(IPAddress.Loopback, port));
        await sender.SendAsync(BuildV1(33, GlobalPositionPayload(), 104),
            new IPEndPoint(IPAddress.Loopback, port));
        await sender.SendAsync(BuildV1(74, VfrHudPayload(mslAltitude: 987.6f), 20),
            new IPEndPoint(IPAddress.Loopback, port));

        await WaitUntilAsync(() => source.IsReady && state.Airspeed > 0);
        Assert.Equal(123.4, state.Altitude, 1);
    }

    [Fact]
    public async Task Stale_udp_link_recovers_from_same_system_without_manual_restart_or_new_heartbeat()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port,
            ExpectedSystemId = 0,
            StaleAfterSeconds = 0.5
        });
        await source.StartAsync();
        using var sender = new UdpClient();

        await sender.SendAsync(BuildV1(0, HeartbeatPayload(), 50, systemId: 7),
            new IPEndPoint(IPAddress.Loopback, port));
        await WaitUntilAsync(() => source.IsReady && state.IsDataValid);
        await WaitUntilAsync(() => !source.IsReady && !state.IsDataValid);
        var staleSequence = state.Sequence;

        // Yeniden başlayan karşı uç ilk heartbeat'i kaçırmış olsa bile aynı system ID'den
        // gelen ilk geçerli telemetri paketi bağlantıyı canlandırmalıdır.
        await sender.SendAsync(BuildV1(33, GlobalPositionPayload(), 104, systemId: 7),
            new IPEndPoint(IPAddress.Loopback, port));

        await WaitUntilAsync(() => source.IsReady && state.IsDataValid && state.Sequence > staleSequence);
    }

    [Fact]
    public async Task Stale_udp_link_discards_partial_frame_before_peer_restarts()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port,
            ExpectedSystemId = 1,
            StaleAfterSeconds = 0.5
        });
        await source.StartAsync();
        using var sender = new UdpClient();

        await sender.SendAsync(BuildV1(0, HeartbeatPayload(), 50),
            new IPEndPoint(IPAddress.Loopback, port));
        await WaitUntilAsync(() => source.IsReady);

        // Karşı süreç frame ortasında kapandı: payload uzunluğu 250 görünen eksik başlık.
        await sender.SendAsync(new byte[] { 0xFE, 250, 2, 1, 1, 33 },
            new IPEndPoint(IPAddress.Loopback, port));
        await WaitUntilAsync(() => !source.IsReady && !state.IsDataValid);
        var staleSequence = state.Sequence;

        await sender.SendAsync(BuildV1(0, HeartbeatPayload(), 50),
            new IPEndPoint(IPAddress.Loopback, port));

        await WaitUntilAsync(() => source.IsReady && state.IsDataValid && state.Sequence > staleSequence);
    }

    [Fact]
    public async Task Mavlink2_truncated_global_position_still_updates_location()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port,
            ExpectedSystemId = 1
        });
        await source.StartAsync();
        using var sender = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        await sender.SendAsync(BuildV1(0, HeartbeatPayload(), 50), endpoint);
        // MAVLink 2; velocity/heading kuyruğu sıfır olduğu için 28 yerine 20 bayt.
        await sender.SendAsync(BuildV2(33, GlobalPositionPayload()[..20], 104), endpoint);

        await WaitUntilAsync(() => Math.Abs(state.Latitude - 41.508775) < 0.000001);
        Assert.Equal(36.118335, state.Longitude, 6);
        Assert.Equal(123.4, state.Altitude, 1);
    }

    [Fact]
    public async Task Gps_raw_int_supplies_position_when_global_position_is_not_published()
    {
        var port = GetFreeUdpPort();
        var state = new FlightState();
        using var source = new MavlinkFlightStateSource(state, new MavlinkOptions
        {
            ListenAddress = "127.0.0.1",
            Port = port,
            ExpectedSystemId = 1
        });
        await source.StartAsync();
        using var sender = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        await sender.SendAsync(BuildV1(0, HeartbeatPayload(), 50), endpoint);
        await sender.SendAsync(BuildV1(24, GpsRawPayload(), 24), endpoint);

        await WaitUntilAsync(() => Math.Abs(state.Latitude - 41.508775) < 0.000001);
        Assert.Equal(36.118335, state.Longitude, 6);
        Assert.Equal(0, state.Altitude); // GPS MSL değeri AGL alanına yazılmaz.
        Assert.Equal(0.9, state.GpsHdop);
        Assert.Equal(23.5, state.GroundSpeed, 1);
        Assert.Equal(123.45, state.GroundTrack);
        Assert.True(state.NavigationSequence > 0);
    }

    private static byte[] HeartbeatPayload()
    {
        var payload = new byte[9];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, 10); // Plane AUTO
        payload[4] = 1;   // MAV_TYPE_FIXED_WING
        payload[5] = 3;   // autopilot
        payload[6] = 0x80;
        payload[7] = 4;
        payload[8] = 3;
        return payload;
    }

    private static byte[] GpsRawPayload()
    {
        var payload = new byte[30];
        var gpsUtc = new DateTimeOffset(2026, 7, 17, 9, 15, 30, TimeSpan.Zero);
        var microseconds = (ulong)gpsUtc.ToUnixTimeMilliseconds() * 1000;
        BinaryPrimitives.WriteUInt64LittleEndian(payload, microseconds);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(8), 415_087_750);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(12), 361_183_350);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(16), 987_600); // MSL mm
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(20), 90); // HDOP 0.90
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(22), 140); // VDOP 1.40
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(24), 2_350); // 23.5 m/s
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(26), 12_345); // 123.45° COG
        payload[28] = 3;
        payload[29] = 14;
        return payload;
    }

    private static byte[] GlobalPositionPayload()
    {
        var payload = new byte[28];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4), 415_087_750);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(8), 361_183_350);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(16), 123_400);
        BinaryPrimitives.WriteInt16LittleEndian(payload.AsSpan(20), 2_500);
        BinaryPrimitives.WriteInt16LittleEndian(payload.AsSpan(22), 0);
        BinaryPrimitives.WriteInt16LittleEndian(payload.AsSpan(24), -120);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(26), 21_000);
        return payload;
    }

    private static byte[] VfrHudPayload(float mslAltitude)
    {
        var payload = new byte[20];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0), BitConverter.SingleToInt32Bits(22.5f));
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4), BitConverter.SingleToInt32Bits(25.0f));
        BinaryPrimitives.WriteInt16LittleEndian(payload.AsSpan(8), 210);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(12), BitConverter.SingleToInt32Bits(mslAltitude));
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(16), BitConverter.SingleToInt32Bits(1.2f));
        return payload;
    }

    private static byte[] BuildV1(byte messageId, byte[] payload, byte crcExtra, byte systemId = 1)
    {
        var frame = new byte[6 + payload.Length + 2];
        frame[0] = 0xFE;
        frame[1] = (byte)payload.Length;
        frame[2] = 1;
        frame[3] = systemId;
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

    private static byte[] BuildV2(int messageId, byte[] payload, byte crcExtra, byte systemId = 1)
    {
        var frame = new byte[10 + payload.Length + 2];
        frame[0] = 0xFD;
        frame[1] = (byte)payload.Length;
        frame[4] = 1;
        frame[5] = systemId;
        frame[6] = 1;
        frame[7] = (byte)messageId;
        frame[8] = (byte)(messageId >> 8);
        frame[9] = (byte)(messageId >> 16);
        payload.CopyTo(frame, 10);

        ushort crc = 0xFFFF;
        foreach (var value in frame.AsSpan(1, 9 + payload.Length))
            AccumulateCrc(value, ref crc);
        AccumulateCrc(crcExtra, ref crc);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(10 + payload.Length), crc);
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
        Assert.True(condition(), "MAVLink paketi belirtilen süre içinde işlenmedi.");
    }
}
