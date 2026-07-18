using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Core.Services.Mavlink;

/// <summary>
/// MAVLink v1/v2 akışından temel Pixhawk telemetrisini okur. RFD868x USB modem
/// için seri portu, SITL/MAVProxy için UDP'yi destekler. Bu adaptör salt-okunurdur;
/// fiziksel uçuş komutları ayrıca uygulanana kadar canlı komut hattı kapalıdır.
/// </summary>
public sealed class MavlinkFlightStateSource : IManagedFlightStateSource
{
    private const byte MavlinkV1Magic = 0xFE;
    private const byte MavlinkV2Magic = 0xFD;
    private const byte ArmedFlag = 0x80;
    private const int MaxReceiveBufferLength = 64 * 1024;
    private static readonly TimeSpan SerialHealthPeriod = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan FastSerialReconnectDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan SlowSerialReconnectDelay = TimeSpan.FromSeconds(1);

    private readonly FlightState _state;
    private readonly MavlinkOptions _options;
    private readonly IMavlinkSerialTransportFactory _serialFactory;
    private readonly object _transportGate = new();
    private readonly object _parserGate = new();
    private UdpClient? _udp;
    private IMavlinkSerialTransport? _serial;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private DateTime? _lastPacketUtc;
    private DateTime? _lastGpsTimeUtc;
    private volatile bool _isReady;
    private int _activeSystemId = -1;
    private byte[] _receiveBuffer = new byte[4096];
    private int _receiveBufferLength;

    public MavlinkFlightStateSource(
        FlightState state,
        MavlinkOptions options,
        IMavlinkSerialTransportFactory? serialFactory = null)
    {
        _state = state;
        _options = options;
        _serialFactory = serialFactory ?? new SystemMavlinkSerialTransportFactory();
    }

    public string Name => $"MAVLink {_options.ConnectionDescription}";
    public bool IsRunning => _loop is not null;
    public bool IsReady => _isReady;
    public event EventHandler<FlightSourceStatusChangedEventArgs>? StatusChanged;

    public void Start() => StartAsync().GetAwaiter().GetResult();

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_loop is not null) return Task.CompletedTask;

        StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
            FlightSourceStatus.Starting, $"{Name} açılıyor"));

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _isReady = false;
        _lastPacketUtc = null;
        _lastGpsTimeUtc = null;
        ResetParserState();
        Volatile.Write(ref _activeSystemId, -1);

        try
        {
            if (_options.Transport == MavlinkTransport.Serial)
            {
                _loop = Task.WhenAll(
                    Task.Run(() => RunSerialSupervisor(_cts.Token), _cts.Token),
                    HealthLoopAsync(_cts.Token));
            }
            else
            {
                OpenUdpListener();
                _loop = Task.WhenAll(
                    ReceiveUdpLoopAsync(_cts.Token),
                    HealthLoopAsync(_cts.Token));

                StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
                    FlightSourceStatus.WaitingForData,
                    $"{Name} dinleniyor; HEARTBEAT bekleniyor"));
            }
        }
        catch
        {
            CloseTransports();
            _cts.Dispose();
            _cts = null;
            throw;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        if (cts is null) return;
        var loop = _loop;

        try
        {
            cts.Cancel();
            // Windows SerialPort, başka thread'deki bloklu Read kapatıldığında
            // IOException yerine InvalidOperationException da fırlatabilir.
            CloseTransports();
            if (loop is not null) await loop.ConfigureAwait(false);
        }
        catch (Exception) when (cts.IsCancellationRequested)
        {
            // Kullanıcının yeniden bağlan komutundaki kontrollü kapanış hatasıdır.
            // Asıl önemli olan finally'de kaynağı tekrar başlatılabilir duruma getirmektir.
        }
        finally
        {
            CloseTransports();
            cts.Dispose();
            if (ReferenceEquals(_cts, cts))
            {
                _cts = null;
                _loop = null;
            }
            _isReady = false;
            _lastPacketUtc = null;
            _lastGpsTimeUtc = null;
            ResetParserState();
            Volatile.Write(ref _activeSystemId, -1);
            _state.MarkUnavailable("MAVLINK");
            StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
                FlightSourceStatus.Stopped, "MAVLink dinleyici durduruldu"));
        }
    }

    private void OpenUdpListener()
    {
        var address = IPAddress.TryParse(_options.ListenAddress, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Geçersiz MAVLink dinleme adresi: {_options.ListenAddress}");

        var udp = new UdpClient();
        try
        {
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(address, _options.Port));
            lock (_transportGate) _udp = udp;
        }
        catch
        {
            udp.Dispose();
            throw;
        }
    }

    private void OpenSerialPort()
    {
        if (string.IsNullOrWhiteSpace(_options.SerialPortName))
            throw new InvalidOperationException("MAVLink seri port adı boş olamaz.");

        var serial = _serialFactory.Open(_options.SerialPortName, _options.BaudRate);

        try
        {
            lock (_transportGate) _serial = serial;
        }
        catch
        {
            serial.Dispose();
            throw;
        }
    }

    private async Task ReceiveUdpLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                UdpClient udp;
                lock (_transportGate) udp = _udp!;
                var result = await udp.ReceiveAsync(ct).ConfigureAwait(false);
                ConsumeBytes(result.Buffer);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            MarkTransportFault($"MAVLink UDP alıcı hatası: {ex.Message}");
        }
    }

    private void RunSerialSupervisor(CancellationToken ct)
    {
        var reconnectAttempt = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!IsConfiguredSerialPortAvailable())
                    throw new IOException($"{_options.SerialPortName} USB/seri portu henüz görünmüyor.");

                OpenSerialPort();
                ResetSerialStreamState();
                reconnectAttempt = 0;
                StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
                    FlightSourceStatus.WaitingForData,
                    "TELEMETRİ BEKLENİYOR"));

                IMavlinkSerialTransport serial;
                lock (_transportGate) serial = _serial!;
                var buffer = new byte[4096];
                while (!ct.IsCancellationRequested)
                {
                    int count;
                    try
                    {
                        count = serial.Read(buffer, 0, buffer.Length);
                    }
                    catch (TimeoutException)
                    {
                        // Veri gelmemesi port kopması değildir. Kısa ReadTimeout sadece
                        // iptal ve USB durumunu yeniden kontrol edebilmek için kullanılır.
                        continue;
                    }
                    if (count == 0)
                        throw new EndOfStreamException("MAVLink seri akışı kapandı.");
                    ConsumeBytes(buffer.AsSpan(0, count));
                }
            }
            catch (Exception) when (ct.IsCancellationRequested) { break; }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                if (HasOpenSerialPort())
                    MarkTransportFault("BAĞLANTI KOPTU");
                else
                    _state.MarkStale("MAVLINK");
            }
            finally
            {
                CloseSerialPort();
            }

            if (!_options.AutoReconnect || ct.IsCancellationRequested) break;
            reconnectAttempt++;
            var delay = reconnectAttempt <= 20
                ? FastSerialReconnectDelay
                : SlowSerialReconnectDelay;
            StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
                FlightSourceStatus.Starting,
                _state.Sequence > 0 ? "BAĞLANTI KOPTU" : "BAĞLANTI BEKLENİYOR"));
            if (ct.WaitHandle.WaitOne(delay)) return;
        }
    }

    private async Task HealthLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(SerialHealthPeriod);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                // Port listeden kaybolduğunda transportu kapat; zaman aşımlı seri
                // okuma supervisor'a döner ve retry döngüsü başlar.
                if (_options.Transport == MavlinkTransport.Serial
                    && _options.AutoReconnect
                    && HasOpenSerialPort()
                    && !IsConfiguredSerialPortAvailable())
                {
                    MarkTransportFault("BAĞLANTI KOPTU");
                    CloseSerialPort();
                    continue;
                }

                // Stale kararı ile parser aynı kilidi kullanır. Tam bu sırada yeni paket
                // gelirse Ready -> Waiting olaylarının ters sıraya düşmesi engellenir.
                lock (_parserGate)
                {
                    if (!_isReady || _lastPacketUtc is not { } last) continue;
                    if (DateTime.UtcNow - last <= TimeSpan.FromSeconds(_options.StaleAfterSeconds)) continue;

                    _isReady = false;
                    _state.MarkStale("MAVLINK");
                    // Karşı uç frame'in ortasında kapanmış olabilir. Eski parçayı korumak,
                    // yeniden başlayan geçerli akışı tamponun arkasında bekletir. System ID
                    // ise aynı uçtan HEARTBEAT gelmeden de toparlanabilmek için korunur.
                    _receiveBufferLength = 0;
                    StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
                        FlightSourceStatus.WaitingForData,
                        "TELEMETRİ BEKLENİYOR"));
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private bool HasOpenSerialPort()
    {
        lock (_transportGate) return _serial is not null;
    }

    private bool IsConfiguredSerialPortAvailable()
    {
        try { return _serialFactory.IsPortAvailable(_options.SerialPortName); }
        catch
        {
            // Port listesinin okunamaması tek başına bağlantıyı kesmemeli;
            // gerçek Open denemesi kesin sonucu verecektir.
            return true;
        }
    }

    private void ResetSerialStreamState()
    {
        ResetParserState();
        _lastPacketUtc = null;
        _lastGpsTimeUtc = null;
        Volatile.Write(ref _activeSystemId, -1);
    }

    /// <summary>
    /// UDP datagramı veya seri porttan parçalı gelen baytları ortak bir akış
    /// tamponunda birleştirir. Böylece bir MAVLink frame'i birden fazla seri okumaya
    /// bölünse bile veri kaybolmaz.
    /// </summary>
    private void ConsumeBytes(ReadOnlySpan<byte> bytes)
    {
        lock (_parserGate) ConsumeBytesCore(bytes);
    }

    private void ConsumeBytesCore(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return;
        if (_receiveBufferLength + bytes.Length > MaxReceiveBufferLength)
        {
            _receiveBufferLength = 0;
            if (bytes.Length > MaxReceiveBufferLength)
                bytes = bytes[^MaxReceiveBufferLength..];
        }

        EnsureReceiveCapacity(_receiveBufferLength + bytes.Length);
        bytes.CopyTo(_receiveBuffer.AsSpan(_receiveBufferLength));
        _receiveBufferLength += bytes.Length;

        var consumed = 0;
        while (consumed < _receiveBufferLength)
        {
            var frameStart = FindMagic(consumed);
            if (frameStart < 0)
            {
                consumed = _receiveBufferLength;
                break;
            }

            var magic = _receiveBuffer[frameStart];
            var minimumLength = magic == MavlinkV1Magic ? 8 : 12;
            if (_receiveBufferLength - frameStart < minimumLength)
            {
                consumed = frameStart;
                break;
            }

            var payloadLength = _receiveBuffer[frameStart + 1];
            var signatureLength = magic == MavlinkV2Magic
                && (_receiveBuffer[frameStart + 2] & 0x01) != 0 ? 13 : 0;
            var frameLength = magic == MavlinkV1Magic
                ? 6 + payloadLength + 2
                : 10 + payloadLength + 2 + signatureLength;
            if (_receiveBufferLength - frameStart < frameLength)
            {
                consumed = frameStart;
                break;
            }

            var systemId = magic == MavlinkV1Magic
                ? _receiveBuffer[frameStart + 3]
                : _receiveBuffer[frameStart + 5];
            var messageId = magic == MavlinkV1Magic
                ? _receiveBuffer[frameStart + 5]
                : _receiveBuffer[frameStart + 7]
                  | (_receiveBuffer[frameStart + 8] << 8)
                  | (_receiveBuffer[frameStart + 9] << 16);

            if (!TryGetCrcExtra(messageId, out var crcExtra))
            {
                // Yapısal olarak tam fakat bu uygulamanın kullanmadığı MAVLink mesajı.
                consumed = frameStart + frameLength;
                continue;
            }

            var headerLength = magic == MavlinkV1Magic ? 5 : 9;
            var payloadOffset = magic == MavlinkV1Magic ? 6 : 10;
            var checksumOffset = frameStart + payloadOffset + payloadLength;
            var headerAndPayload = _receiveBuffer.AsSpan(
                frameStart + 1,
                headerLength + payloadLength);
            var checksum = _receiveBuffer.AsSpan(checksumOffset, 2);

            if (HasValidChecksum(headerAndPayload, checksum, crcExtra))
            {
                ProcessMessage(
                    systemId,
                    messageId,
                    _receiveBuffer.AsSpan(frameStart + payloadOffset, payloadLength));
                consumed = frameStart + frameLength;
            }
            else
            {
                // Bozuk frame'den sonra geçerli bir magic baytı bulabilmek için bir bayt kaydır.
                consumed = frameStart + 1;
            }
        }

        if (consumed <= 0) return;
        var remaining = _receiveBufferLength - consumed;
        if (remaining > 0)
            Buffer.BlockCopy(_receiveBuffer, consumed, _receiveBuffer, 0, remaining);
        _receiveBufferLength = remaining;
    }

    private void ResetParserState()
    {
        lock (_parserGate) _receiveBufferLength = 0;
    }

    private int FindMagic(int start)
    {
        for (var i = start; i < _receiveBufferLength; i++)
        {
            if (_receiveBuffer[i] is MavlinkV1Magic or MavlinkV2Magic) return i;
        }
        return -1;
    }

    private void EnsureReceiveCapacity(int required)
    {
        if (required <= _receiveBuffer.Length) return;
        var size = _receiveBuffer.Length;
        while (size < required) size *= 2;
        Array.Resize(ref _receiveBuffer, Math.Min(size, MaxReceiveBufferLength));
    }

    private static bool HasValidChecksum(
        ReadOnlySpan<byte> headerAndPayload,
        ReadOnlySpan<byte> checksum,
        byte crcExtra)
    {
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
        if (!ShouldAcceptSystem(systemId, messageId)) return;

        var processed = messageId switch
        {
            0 => ReadHeartbeat(systemId, payload),
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
        MarkReady(systemId);
        _lastPacketUtc = DateTime.UtcNow;
        _state.Touch("MAVLINK", _lastGpsTimeUtc);
        if (messageId is 24 or 33)
            _state.TouchNavigation();
    }

    private bool ShouldAcceptSystem(byte systemId, int messageId)
    {
        if (_options.ExpectedSystemId is > 0)
            return systemId == _options.ExpectedSystemId;

        var active = Volatile.Read(ref _activeSystemId);
        if (messageId == 0 && (active < 0 || !_isReady))
        {
            // İlk bağlantıda veya stale sonrası gelen HEARTBEAT yeni otoritedir.
            // Böylece karşı süreç farklı system ID ile yeniden açılsa da toparlanır.
            Volatile.Write(ref _activeSystemId, systemId);
            active = Volatile.Read(ref _activeSystemId);
        }
        return active == systemId;
    }

    private bool ReadHeartbeat(byte systemId, ReadOnlySpan<byte> p)
    {
        if (p.Length < 9) return false;
        var customMode = BinaryPrimitives.ReadUInt32LittleEndian(p);
        var vehicleType = p[4];
        var baseMode = p[6];
        _state.IsArmed = (baseMode & ArmedFlag) != 0;
        _state.Mode = MapMode(vehicleType, customMode);
        _state.IsAutonomous = _state.Mode is FlightMode.Auto or FlightMode.Guided;

        return true;
    }

    private void MarkReady(byte systemId)
    {
        if (_isReady) return;
        _isReady = true;
        StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
            FlightSourceStatus.Ready,
            $"MAVLink canlı · system {systemId} · {_options.ConnectionDescription}"));
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
        // MAVLink 2 sondaki sıfır baytlarını kırpabilir; fix_type alanına kadar
        // gelmiş bir paket geçerlidir, satellites_visible eksikse 0 kabul edilir.
        if (p.Length < 29) return false;
        Span<byte> full = stackalloc byte[30];
        p[..Math.Min(p.Length, full.Length)].CopyTo(full);

        var timeUsec = BinaryPrimitives.ReadUInt64LittleEndian(full);
        var fix = MapGpsFix(full[28]);
        _state.GpsFix = fix;
        _state.SatelliteCount = p.Length > 29 && full[29] != byte.MaxValue ? full[29] : 0;
        var eph = BinaryPrimitives.ReadUInt16LittleEndian(full.Slice(20, 2));
        _state.GpsHdop = eph != ushort.MaxValue && eph > 0 ? eph / 100.0 : null;

        var velocityCentimetersPerSecond = BinaryPrimitives.ReadUInt16LittleEndian(full.Slice(24, 2));
        if (velocityCentimetersPerSecond != ushort.MaxValue)
            _state.GroundSpeed = velocityCentimetersPerSecond / 100.0;

        var courseCentidegrees = BinaryPrimitives.ReadUInt16LittleEndian(full.Slice(26, 2));
        _state.GroundTrack = courseCentidegrees != ushort.MaxValue
            ? courseCentidegrees / 100.0
            : null;
        _lastGpsTimeUtc = TryConvertUnixMicroseconds(timeUsec);

        // GLOBAL_POSITION_INT henüz gelmediyse harita boş kalmasın. GPS_RAW_INT
        // enlem/boylamı güvenli yedektir; MSL irtifası AGL alanını ezmez.
        if (fix is GpsFix.Fix2D or GpsFix.Fix3D or GpsFix.Dgps or GpsFix.Rtk)
        {
            var rawLatitude = BinaryPrimitives.ReadInt32LittleEndian(full.Slice(8, 4));
            var rawLongitude = BinaryPrimitives.ReadInt32LittleEndian(full.Slice(12, 4));
            ApplyPositionIfUsable(rawLatitude, rawLongitude);
        }
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
        // MAVLink 2 payload'ın sonundaki sıfır alanları göndermez. Konum için ilk
        // 12 bayt yeterlidir; eksik kuyruk protokol gereği sıfırla tamamlanır.
        if (p.Length < 12) return false;
        Span<byte> full = stackalloc byte[28];
        p[..Math.Min(p.Length, full.Length)].CopyTo(full);

        var rawLatitude = BinaryPrimitives.ReadInt32LittleEndian(full.Slice(4, 4));
        var rawLongitude = BinaryPrimitives.ReadInt32LittleEndian(full.Slice(8, 4));
        ApplyPositionIfUsable(rawLatitude, rawLongitude);
        // API yere göre irtifa ister; GLOBAL_POSITION_INT.relative_alt tam olarak bu alandır.
        _state.Altitude = BinaryPrimitives.ReadInt32LittleEndian(full.Slice(16, 4)) / 1000.0;
        var vx = BinaryPrimitives.ReadInt16LittleEndian(full.Slice(20, 2)) / 100.0;
        var vy = BinaryPrimitives.ReadInt16LittleEndian(full.Slice(22, 2)) / 100.0;
        var vz = BinaryPrimitives.ReadInt16LittleEndian(full.Slice(24, 2)) / 100.0;
        _state.GroundSpeed = Math.Sqrt(vx * vx + vy * vy);
        _state.VerticalSpeed = -vz;
        if (_state.GroundSpeed >= 1.0)
        {
            // MAVLink GLOBAL_POSITION_INT: vx kuzey, vy doğu. Harita açısı
            // kuzeyden saat yönünde olduğu için atan2(doğu, kuzey) kullanılır.
            _state.GroundTrack = NormalizeHeading(
                Math.Atan2(vy, vx) * 180.0 / Math.PI);
        }
        var heading = BinaryPrimitives.ReadUInt16LittleEndian(full.Slice(26, 2));
        if (heading != ushort.MaxValue) _state.Heading = heading / 100.0;
        return true;
    }

    private void ApplyPositionIfUsable(int rawLatitude, int rawLongitude)
    {
        var latitude = rawLatitude / 1e7;
        var longitude = rawLongitude / 1e7;
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) return;
        if (Math.Abs(latitude) <= 0.000001 && Math.Abs(longitude) <= 0.000001) return;
        _state.Latitude = latitude;
        _state.Longitude = longitude;
    }

    private bool ReadVfrHud(ReadOnlySpan<byte> p)
    {
        if (p.Length < 20) return false;
        _state.Airspeed = ReadSingle(p, 0);
        _state.GroundSpeed = ReadSingle(p, 4);
        _state.Heading = NormalizeHeading(BinaryPrimitives.ReadInt16LittleEndian(p.Slice(8, 2)));
        // VFR_HUD.alt deniz seviyesine göre olabilir. Yarışma API'sinin AGL alanını
        // bozmamak için irtifa yalnızca GLOBAL_POSITION_INT.relative_alt'tan alınır.
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
            if (totalMillivolts > 0)
                _state.BatteryVoltage = (int)Math.Round(totalMillivolts / 1000.0);
        }
        if (remaining is >= 0 and <= 100) _state.BatteryPercent = remaining;
        return true;
    }

    private void MarkTransportFault(string message)
    {
        _isReady = false;
        _lastPacketUtc = null;
        _lastGpsTimeUtc = null;
        ResetParserState();
        Volatile.Write(ref _activeSystemId, -1);
        _state.MarkStale("MAVLINK");
        StatusChanged?.Invoke(this, new FlightSourceStatusChangedEventArgs(
            FlightSourceStatus.Faulted, message));
    }

    private void CloseTransports()
    {
        UdpClient? udp;
        IMavlinkSerialTransport? serial;
        lock (_transportGate)
        {
            udp = _udp;
            serial = _serial;
            _udp = null;
            _serial = null;
        }
        try { udp?.Dispose(); } catch { }
        try { serial?.Dispose(); } catch { }
    }

    private void CloseSerialPort()
    {
        IMavlinkSerialTransport? serial;
        lock (_transportGate)
        {
            serial = _serial;
            _serial = null;
        }
        try { serial?.Dispose(); } catch { }
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
        CloseTransports();
        try { _loop?.Wait(TimeSpan.FromMilliseconds(500)); } catch { }
        try { _cts.Dispose(); } catch { }
        _cts = null;
        _loop = null;
        _isReady = false;
        Volatile.Write(ref _activeSystemId, -1);
    }
}
