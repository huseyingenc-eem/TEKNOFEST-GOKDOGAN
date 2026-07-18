using System.IO.Ports;

namespace GOKDOGANIHA.Core.Services.Mavlink;

/// <summary>
/// Seri MAVLink hattını donanımdan ayırır. Üretim uygulamasında
/// <see cref="SerialPort"/>, testlerde kontrollü bir bellek akışı kullanılır.
/// </summary>
public interface IMavlinkSerialTransport : IDisposable
{
    int Read(byte[] buffer, int offset, int count);
}

public interface IMavlinkSerialTransportFactory
{
    bool IsPortAvailable(string portName);
    IMavlinkSerialTransport Open(string portName, int baudRate);
}

internal sealed class SystemMavlinkSerialTransportFactory : IMavlinkSerialTransportFactory
{
    public bool IsPortAvailable(string portName)
        => SerialPort.GetPortNames().Contains(portName, StringComparer.OrdinalIgnoreCase);

    public IMavlinkSerialTransport Open(string portName, int baudRate)
    {
        var serial = new SerialPort(
            portName,
            baudRate,
            Parity.None,
            dataBits: 8,
            StopBits.One)
        {
            Handshake = Handshake.None,
            DtrEnable = false,
            RtsEnable = false,
            ReadTimeout = 500,
            WriteTimeout = 500
        };

        try
        {
            serial.Open();
            return new SystemMavlinkSerialTransport(serial);
        }
        catch
        {
            serial.Dispose();
            throw;
        }
    }

    private sealed class SystemMavlinkSerialTransport : IMavlinkSerialTransport
    {
        private readonly SerialPort _serial;

        public SystemMavlinkSerialTransport(SerialPort serial) => _serial = serial;
        public int Read(byte[] buffer, int offset, int count)
            => _serial.Read(buffer, offset, count);
        public void Dispose() => _serial.Dispose();
    }
}
