using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Threading.Channels;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Mavlink;

namespace GOKDOGANIHA.Tests.Services;

public class MavlinkSerialReconnectTests
{
    [Fact]
    public async Task Usb_removal_releases_stuck_read_and_reconnects_without_restarting_application()
    {
        var factory = new FakeSerialFactory { IsAvailable = true };
        var state = new FlightState();
        using var source = CreateSource(state, factory);
        var statuses = new ConcurrentQueue<string>();
        source.StatusChanged += (_, status) => statuses.Enqueue(status.Message);

        await source.StartAsync();
        await WaitUntilAsync(() => factory.OpenCount == 1);
        factory.Latest.Write(BuildHeartbeat());
        await WaitUntilAsync(() => source.IsReady && state.Sequence == 1);

        // Gerçek Windows davranışını taklit eder: USB port listeden kaybolur
        // fakat eski seri okuma kendiliğinden tamamlanmaz.
        var firstTransport = factory.Latest;
        factory.IsAvailable = false;
        await WaitUntilAsync(() => firstTransport.IsDisposed && !state.IsDataValid);

        var reinsertedAt = DateTime.UtcNow;
        factory.IsAvailable = true;
        await WaitUntilAsync(() => factory.OpenCount >= 2);
        factory.Latest.Write(BuildHeartbeat());
        await WaitUntilAsync(() => source.IsReady && state.Sequence == 2);

        Assert.True(source.IsRunning);
        Assert.InRange(DateTime.UtcNow - reinsertedAt, TimeSpan.Zero, TimeSpan.FromSeconds(1.5));
        Assert.Contains(statuses, message => message == "BAĞLANTI KOPTU");
    }

    [Fact]
    public async Task Source_started_while_usb_is_missing_connects_when_port_appears_later()
    {
        var factory = new FakeSerialFactory { IsAvailable = false };
        var state = new FlightState();
        using var source = CreateSource(state, factory);
        await source.StartAsync();
        await Task.Delay(350);

        Assert.True(source.IsRunning);
        Assert.Equal(0, factory.OpenCount);
        Assert.False(source.IsReady);

        var insertedAt = DateTime.UtcNow;
        factory.IsAvailable = true;
        await WaitUntilAsync(() => factory.OpenCount == 1);
        factory.Latest.Write(BuildHeartbeat());
        await WaitUntilAsync(() => source.IsReady);

        Assert.InRange(DateTime.UtcNow - insertedAt, TimeSpan.Zero, TimeSpan.FromSeconds(1.5));
        Assert.True(state.IsDataValid);
    }

    [Fact]
    public async Task Driver_read_error_reconnects_even_when_port_name_is_already_visible_again()
    {
        var factory = new FakeSerialFactory { IsAvailable = true };
        var state = new FlightState();
        using var source = CreateSource(state, factory);

        await source.StartAsync();
        await WaitUntilAsync(() => factory.OpenCount == 1);
        factory.Latest.Write(BuildHeartbeat());
        await WaitUntilAsync(() => source.IsReady);

        // Hızlı sök-takta COM adı tekrar görünürken eski Windows handle'ı
        // okuma hatası verir. Bu hata stale timeout beklemeden yeniden açmalı.
        var oldTransport = factory.Latest;
        oldTransport.Disconnect();
        await WaitUntilAsync(() => factory.OpenCount >= 2);
        factory.Latest.Write(BuildHeartbeat());
        await WaitUntilAsync(() => source.IsReady && state.Sequence == 2);

        Assert.True(state.IsDataValid);
    }

    [Fact]
    public async Task Telemetry_silence_marks_link_stale_but_keeps_last_values_and_does_not_reopen_port()
    {
        var factory = new FakeSerialFactory { IsAvailable = true };
        var state = new FlightState();
        using var source = CreateSource(state, factory);
        var statuses = new ConcurrentQueue<string>();
        source.StatusChanged += (_, status) => statuses.Enqueue(status.Message);

        await source.StartAsync();
        await WaitUntilAsync(() => factory.OpenCount == 1);
        var transport = factory.Latest;
        transport.Write(BuildHeartbeat());
        await WaitUntilAsync(() => source.IsReady && state.IsDataValid);
        var lastUpdate = state.LastUpdatedUtc;
        var lastMode = state.Mode;

        await WaitUntilAsync(() => !source.IsReady && !state.IsDataValid);

        Assert.Equal(1, factory.OpenCount);
        Assert.False(transport.IsDisposed);
        Assert.Equal(lastUpdate, state.LastUpdatedUtc);
        Assert.Equal(lastMode, state.Mode);
        Assert.Equal(1, state.Sequence);
        Assert.Contains(statuses, message => message == "TELEMETRİ BEKLENİYOR");
    }

    [Fact]
    public async Task Manual_retry_recovers_when_windows_read_throws_invalid_operation_during_stop()
    {
        var factory = new FakeSerialFactory
        {
            IsAvailable = true,
            ThrowInvalidOperationWhenDisposed = true
        };
        var state = new FlightState();
        using var source = CreateSource(state, factory);

        await source.StartAsync();
        await WaitUntilAsync(() => factory.OpenCount == 1);
        factory.Latest.Write(BuildHeartbeat());
        await WaitUntilAsync(() => source.IsReady);

        // SerialPort.Read gerçek Windows sürücüsünde Close ile yarıştığında
        // InvalidOperationException verebilir. Stop yine de tüm alanları temizlemeli.
        await source.StopAsync();
        Assert.False(source.IsRunning);
        Assert.False(source.IsReady);

        await source.StartAsync();
        await WaitUntilAsync(() => factory.OpenCount == 2);
        factory.Latest.Write(BuildHeartbeat());
        await WaitUntilAsync(() => source.IsReady && state.Sequence == 2);
    }

    private static MavlinkFlightStateSource CreateSource(
        FlightState state,
        IMavlinkSerialTransportFactory factory)
        => new(state, new MavlinkOptions
        {
            Transport = MavlinkTransport.Serial,
            SerialPortName = "COM77",
            BaudRate = 57600,
            ExpectedSystemId = 1,
            AutoReconnect = true,
            StaleAfterSeconds = 1
        }, factory);

    private static byte[] BuildHeartbeat()
    {
        var payload = new byte[9];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, 10);
        payload[4] = 1;
        payload[6] = 0x80;
        payload[8] = 3;

        var frame = new byte[6 + payload.Length + 2];
        frame[0] = 0xFE;
        frame[1] = (byte)payload.Length;
        frame[2] = 1;
        frame[3] = 1;
        frame[4] = 1;
        frame[5] = 0;
        payload.CopyTo(frame, 6);

        ushort crc = 0xFFFF;
        foreach (var value in frame.AsSpan(1, 5 + payload.Length))
            AccumulateCrc(value, ref crc);
        AccumulateCrc(50, ref crc);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(6 + payload.Length), crc);
        return frame;
    }

    private static void AccumulateCrc(byte value, ref ushort crc)
    {
        var tmp = (byte)(value ^ (byte)(crc & 0xFF));
        tmp ^= (byte)(tmp << 4);
        crc = (ushort)((crc >> 8) ^ (tmp << 8) ^ (tmp << 3) ^ (tmp >> 4));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(20);
        Assert.True(condition(), "Seri MAVLink yeniden bağlanma koşulu zamanında gerçekleşmedi.");
    }

    private sealed class FakeSerialFactory : IMavlinkSerialTransportFactory
    {
        private readonly object _gate = new();
        private readonly List<FakeSerialTransport> _opened = new();
        private int _available;

        public bool IsAvailable
        {
            get => Volatile.Read(ref _available) == 1;
            set => Volatile.Write(ref _available, value ? 1 : 0);
        }

        public int OpenCount { get { lock (_gate) return _opened.Count; } }
        public bool ThrowInvalidOperationWhenDisposed { get; init; }
        public FakeSerialTransport Latest
        {
            get { lock (_gate) return _opened[^1]; }
        }

        public bool IsPortAvailable(string portName) => IsAvailable;

        public IMavlinkSerialTransport Open(string portName, int baudRate)
        {
            if (!IsAvailable) throw new IOException($"{portName} bulunamadı.");
            var transport = new FakeSerialTransport(ThrowInvalidOperationWhenDisposed);
            lock (_gate) _opened.Add(transport);
            return transport;
        }
    }

    private sealed class FakeSerialTransport : IMavlinkSerialTransport
    {
        private readonly BlockingInput _input;
        public FakeSerialTransport(bool throwInvalidOperationWhenDisposed = false)
            => _input = new BlockingInput(throwInvalidOperationWhenDisposed);
        public bool IsDisposed => _input.IsDisposed;
        public int Read(byte[] buffer, int offset, int count) => _input.Read(buffer, offset, count);
        public void Write(byte[] bytes) => _input.Write(bytes);
        public void Disconnect() => Dispose();
        public void Dispose() => _input.Dispose();
    }

    private sealed class BlockingInput : IDisposable
    {
        private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>();
        private readonly bool _throwInvalidOperationWhenDisposed;
        private byte[]? _current;
        private int _offset;
        private int _disposed;

        public BlockingInput(bool throwInvalidOperationWhenDisposed)
            => _throwInvalidOperationWhenDisposed = throwInvalidOperationWhenDisposed;

        public bool IsDisposed => Volatile.Read(ref _disposed) == 1;
        public void Write(byte[] bytes)
        {
            if (!_channel.Writer.TryWrite(bytes))
                throw new IOException("Test seri akışı kapalı.");
        }

        public int Read(byte[] buffer, int destinationOffset, int count)
        {
            while (true)
            {
                if (_current is not null && _offset < _current.Length)
                {
                    var copied = Math.Min(count, _current.Length - _offset);
                    Array.Copy(_current, _offset, buffer, destinationOffset, copied);
                    _offset += copied;
                    return copied;
                }

                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                try
                {
                    if (!_channel.Reader.WaitToReadAsync(timeout.Token).AsTask().GetAwaiter().GetResult())
                    {
                        if (_throwInvalidOperationWhenDisposed)
                            throw new InvalidOperationException("Test seri portu kapatıldı.");
                        throw new IOException("Test USB seri hattı kapandı.");
                    }
                    if (!_channel.Reader.TryRead(out _current))
                        continue;
                    _offset = 0;
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException();
                }
                catch (ChannelClosedException ex)
                {
                    if (_throwInvalidOperationWhenDisposed)
                        throw new InvalidOperationException("Test seri portu kapatıldı.", ex);
                    throw new IOException("Test USB seri hattı kapandı.", ex);
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _channel.Writer.TryComplete();
        }
    }
}
