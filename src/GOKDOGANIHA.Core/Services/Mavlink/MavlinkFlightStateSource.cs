using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Core.Services.Mavlink;

/// <summary>
/// MAVLink v1/v2 UDP datagramlarından temel Pixhawk telemetrisini okur.
/// Bu adaptör salt-okunurdur; fiziksel uçuş komutları ayrıca uygulanana kadar
/// canlı command sink güvenli biçimde blocked kalır.
/// </summary>
public sealed class MavlinkFlightStateSource : IManagedFlightStateSource
{
    private const byte MavlinkV1Magic = 0xFE;
    private const byte MavlinkV2Magic = 0xFD;
    private const byte ArmedFlag = 0x80;

    private readonly FlightState _state;
    private readonly MavlinkOptions _options;
    private UdpClient? _udp;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private DateTime? _lastPacketUtc;
    private DateTime? _lastGpsTimeUtc;
    private bool _isReady;

    public MavlinkFlightStateSource(FlightState state, MavlinkOptions options)
    {
        _state = state;
        _options = options;
    }

    public string Name => $"MAVLink UDP {_options.ListenAddress}:{_options.Port}";
    public bool IsRunning => _loop is not null;
    public bool IsReady => _isReady;
    public event EventHandler<FlightSourceStatusChangedEventArgs>? StatusChanged;

    public void Start() => StartAsync().GetAwaiter().GetResult();

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_loop is not null) return Task.CompletedTask;

        StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
            FlightSourceStatus.Starting, $"{Name} açılıyor"));

        var address = IPAddress.TryParse(_options.ListenAddress, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Geçersiz MAVLink dinleme adresi: {_options.ListenAddress}");

        _udp = new UdpClient();
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.Client.Bind(new IPEndPoint(address, _options.Port));
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _isReady = false;
        _lastPacketUtc = null;
        _loop = Task.WhenAll(ReceiveLoopAsync(_cts.Token), HealthLoopAsync(_cts.Token));

        StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
            FlightSourceStatus.WaitingForData,
            $"{Name} dinleniyor; HEARTBEAT bekleniyor"));
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        _cts.Cancel();
        _udp?.Dispose();
        try { if (_loop is not null) await _loop.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        _cts.Dispose();
        _cts = null;
        _udp = null;
        _loop = null;
        _isReady = false;
        StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
            FlightSourceStatus.Stopped, "MAVLink dinleyici durduruldu"));
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await _udp!.ReceiveAsync(ct).ConfigureAwait(false);
                ParseDatagram(result.Buffer);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _isReady = false;
            _state.MarkUnavailable("MAVLINK");
            StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
                FlightSourceStatus.Faulted, $"MAVLink alıcı hatası: {ex.Message}"));
        }
    }

    private async Task HealthLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (!_isReady || _lastPacketUtc is not { } last) continue;
                if (DateTime.UtcNow - last <= TimeSpan.FromSeconds(_options.StaleAfterSeconds)) continue;

                _isReady = false;
                _state.MarkUnavailable("MAVLINK");
                StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
                    FlightSourceStatus.WaitingForData,
                    $"MAVLink telemetrisi {_options.StaleAfterSeconds:0.0} sn boyunca alınamadı"));
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ParseDatagram(ReadOnlySpan<byte> datagram)
    {
        var offset = 0;
        while (offset < datagram.Length)
        {
            var magic = datagram[offset];
            if (magic is not (MavlinkV1Magic or MavlinkV2Magic))
            {
                offset++;
                continue;
            }

            if (magic == MavlinkV1Magic)
            {
                if (offset + 8 > datagram.Length) return;
                var payloadLength = datagram[offset + 1];
                var frameLength = 6 + payloadLength + 2;
                if (offset + frameLength > datagram.Length) return;
                var systemId = datagram[offset + 3];
                var messageId = datagram[offset + 5];
                if (HasValidChecksum(
                    messageId,
                    datagram.Slice(offset + 1, 5 + payloadLength),
                    datagram.Slice(offset + 6 + payloadLength, 2)))
                {
                    ProcessMessage(systemId, messageId, datagram.Slice(offset + 6, payloadLength));
                }
                offset += frameLength;
                continue;
            }

            if (offset + 12 > datagram.Length) return;
            var v2PayloadLength = datagram[offset + 1];
            var incompatFlags = datagram[offset + 2];
            var signatureLength = (incompatFlags & 0x01) != 0 ? 13 : 0;
            var v2FrameLength = 10 + v2PayloadLength + 2 + signatureLength;
            if (offset + v2FrameLength > datagram.Length) return;
            var v2SystemId = datagram[offset + 5];
            var v2MessageId = datagram[offset + 7]
                | (datagram[offset + 8] << 8)
                | (datagram[offset + 9] << 16);
            if (HasValidChecksum(
                v2MessageId,
                datagram.Slice(offset + 1, 9 + v2PayloadLength),
                datagram.Slice(offset + 10 + v2PayloadLength, 2)))
            {
                ProcessMessage(v2SystemId, v2MessageId, datagram.Slice(offset + 10, v2PayloadLength));
            }
            offset += v2FrameLength;
        }
    }

    private static bool HasValidChecksum(int messageId, ReadOnlySpan<byte> headerAndPayload, ReadOnlySpan<byte> checksum)
    {
        if (!TryGetCrcExtra(messageId, out var crcExtra)) return false;
        ushort crc = 0xFFFF;
        foreach (var value in headerAndPayload) AccumulateCrc(value, ref crc);
        AccumulateCrc(crcExtra, ref crc);
        return checksum.Length >= 2
            && BinaryPrimitives.ReadUInt16LittleEndian(checksum) == crc;
    }

    private static void AccumulateCrc(byte value, ref ushort crc)
    {
        var tmp = (byte)(value ^ (byte)(crc & 0xFF));
        tmp ^= (byte)(tmp << 4);
        crc = (ushort)((crc >> 8)
            ^ (tmp << 8)
            ^ (tmp << 3)
            ^ (tmp >> 4));
    }

    private static bool TryGetCrcExtra(int messageId, out byte crcExtra)
    {
        crcExtra = messageId switch
        {
            0 => 50,
            1 => 124,
            24 => 24,
            30 => 39,
            33 => 104,
            74 => 20,
            109 => 185,
            147 => 154,
            _ => 0
        };
        return messageId is 0 or 1 or 24 or 30 or 33 or 74 or 109 or 147;
    }

    private void ProcessMessage(byte systemId, int messageId, ReadOnlySpan<byte> payload)
    {
        if (_options.ExpectedSystemId is > 0 && systemId != _options.ExpectedSystemId) return;

        var processed = messageId switch
        {
            0 => ReadHeartbeat(payload),
            1 => ReadSystemStatus(payload),
            24 => ReadGpsRaw(payload),
            30 => ReadAttitude(payload),
            33 => ReadGlobalPosition(payload),
            74 => ReadVfrHud(payload),
            109 => ReadRadioStatus(payload),
            147 => ReadBatteryStatus(payload),
            _ => false
        };

        if (!processed) return;
        _lastPacketUtc = DateTime.UtcNow;
        _state.Touch("MAVLINK", _lastGpsTimeUtc);
    }

    private bool ReadHeartbeat(ReadOnlySpan<byte> p)
    {
        if (p.Length < 9) return false;
        var customMode = BinaryPrimitives.ReadUInt32LittleEndian(p);
        var vehicleType = p[4];
        var baseMode = p[6];
        _state.IsArmed = (baseMode & ArmedFlag) != 0;
        _state.Mode = MapMode(vehicleType, customMode);
        _state.IsAutonomous = _state.Mode is FlightMode.Auto or FlightMode.Guided;

        if (!_isReady)
        {
            _isReady = true;
            var systemLabel = _options.ExpectedSystemId > 0
                ? _options.ExpectedSystemId.ToString()
                : "auto";
            StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
                FlightSourceStatus.Ready, $"MAVLink canlı · system {systemLabel}"));
        }
        return true;
    }

    private bool ReadSystemStatus(ReadOnlySpan<byte> p)
    {
        if (p.Length < 31) return false;
        var millivolts = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(14, 2));
        var remaining = unchecked((sbyte)p[30]);
        if (millivolts is > 0 and < ushort.MaxValue)
            _state.BatteryVoltage = (int)Math.Round(millivolts / 1000.0);
        if (remaining is >= 0 and <= 100) _state.BatteryPercent = remaining;
        return true;
    }

    private bool ReadGpsRaw(ReadOnlySpan<byte> p)
    {
        if (p.Length < 30) return false;
        var timeUsec = BinaryPrimitives.ReadUInt64LittleEndian(p);
        _state.GpsFix = MapGpsFix(p[28]);
        _state.SatelliteCount = p[29] == byte.MaxValue ? 0 : p[29];
        _lastGpsTimeUtc = TryConvertUnixMicroseconds(timeUsec);
        return true;
    }

    private bool ReadAttitude(ReadOnlySpan<byte> p)
    {
        if (p.Length < 16) return false;
        _state.Roll = RadiansToDegrees(ReadSingle(p, 4));
        _state.Pitch = RadiansToDegrees(ReadSingle(p, 8));
        _state.Heading = NormalizeHeading(RadiansToDegrees(ReadSingle(p, 12)));
        return true;
    }

    private bool ReadGlobalPosition(ReadOnlySpan<byte> p)
    {
        if (p.Length < 28) return false;
        _state.Latitude = BinaryPrimitives.ReadInt32LittleEndian(p.Slice(4, 4)) / 1e7;
        _state.Longitude = BinaryPrimitives.ReadInt32LittleEndian(p.Slice(8, 4)) / 1e7;
        _state.Altitude = BinaryPrimitives.ReadInt32LittleEndian(p.Slice(16, 4)) / 1000.0;
        var vx = BinaryPrimitives.ReadInt16LittleEndian(p.Slice(20, 2)) / 100.0;
        var vy = BinaryPrimitives.ReadInt16LittleEndian(p.Slice(22, 2)) / 100.0;
        var vz = BinaryPrimitives.ReadInt16LittleEndian(p.Slice(24, 2)) / 100.0;
        _state.GroundSpeed = Math.Sqrt(vx * vx + vy * vy);
        _state.VerticalSpeed = -vz;
        var heading = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(26, 2));
        if (heading != ushort.MaxValue) _state.Heading = heading / 100.0;
        return true;
    }

    private bool ReadVfrHud(ReadOnlySpan<byte> p)
    {
        if (p.Length < 20) return false;
        _state.Airspeed = ReadSingle(p, 0);
        _state.GroundSpeed = ReadSingle(p, 4);
        _state.Heading = NormalizeHeading(BinaryPrimitives.ReadInt16LittleEndian(p.Slice(8, 2)));
        _state.Altitude = ReadSingle(p, 12);
        _state.VerticalSpeed = ReadSingle(p, 16);
        return true;
    }

    private bool ReadRadioStatus(ReadOnlySpan<byte> p)
    {
        if (p.Length < 1) return false;
        _state.SignalRssi = -120 + (p[0] / 255.0 * 100);
        return true;
    }

    private bool ReadBatteryStatus(ReadOnlySpan<byte> p)
    {
        if (p.Length < 36) return false;
        var firstCellMillivolts = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(5, 2));
        var remaining = unchecked((sbyte)p[35]);
        if (firstCellMillivolts is > 0 and < ushort.MaxValue)
        {
            var totalMillivolts = 0;
            for (var i = 0; i < 10; i++)
            {
                var cell = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(5 + i * 2, 2));
                if (cell is 0 or ushort.MaxValue) break;
                totalMillivolts += cell;
            }
            if (totalMillivolts > 0) _state.BatteryVoltage = (int)Math.Round(totalMillivolts / 1000.0);
        }
        if (remaining is >= 0 and <= 100) _state.BatteryPercent = remaining;
        return true;
    }

    private static float ReadSingle(ReadOnlySpan<byte> p, int offset)
        => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(p.Slice(offset, 4)));

    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;
    private static double NormalizeHeading(double heading) => (heading % 360 + 360) % 360;

    private static DateTime? TryConvertUnixMicroseconds(ulong value)
    {
        const ulong year2000UnixMicroseconds = 946_684_800_000_000;
        if (value < year2000UnixMicroseconds) return null;
        try { return DateTimeOffset.FromUnixTimeMilliseconds((long)(value / 1000)).UtcDateTime; }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    private static GpsFix MapGpsFix(byte fix) => fix switch
    {
        0 => GpsFix.None,
        1 => GpsFix.NoFix,
        2 => GpsFix.Fix2D,
        3 => GpsFix.Fix3D,
        4 => GpsFix.Dgps,
        >= 5 => GpsFix.Rtk
    };

    private static FlightMode MapMode(byte vehicleType, uint customMode)
    {
        var fixedWing = vehicleType is 1 or 19 or 20 or 21;
        if (fixedWing)
        {
            return customMode switch
            {
                0 => FlightMode.Manual,
                1 => FlightMode.Circle,
                2 => FlightMode.Stabilize,
                5 => FlightMode.FlyByWireA,
                6 => FlightMode.FlyByWireB,
                10 => FlightMode.Auto,
                11 => FlightMode.Rtl,
                12 => FlightMode.Loiter,
                15 => FlightMode.Guided,
                20 => FlightMode.Takeoff,
                _ => FlightMode.Unknown
            };
        }

        return customMode switch
        {
            0 => FlightMode.Stabilize,
            3 => FlightMode.Auto,
            4 => FlightMode.Guided,
            5 => FlightMode.Loiter,
            6 => FlightMode.Rtl,
            9 => FlightMode.Land,
            _ => FlightMode.Unknown
        };
    }

    public void Dispose()
    {
        if (_cts is null) return;
        try { _cts.Cancel(); } catch { }
        try { _udp?.Dispose(); } catch { }
        try { _loop?.Wait(TimeSpan.FromMilliseconds(500)); } catch { }
        try { _cts.Dispose(); } catch { }
        _cts = null;
        _udp = null;
        _loop = null;
        _isReady = false;
    }
}
