using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Pixhawk/SITL MAVLink UDP dinleyici ayarları. İlk sürüm salt-okunur telemetri
/// alır; fiziksel uçuş komutları ayrı ve güvenli bir adaptör gelene kadar kapalıdır.
/// </summary>
public sealed class MavlinkOptions : INotifyPropertyChanged
{
    private MavlinkTransport _transport = MavlinkTransport.Udp;
    private string _listenAddress = "0.0.0.0";
    private int _port = 14550;
    private string _serialPortName = "COM3";
    private int _baudRate = 57600;
    private int _expectedSystemId;
    private double _staleAfterSeconds = 3;
    private bool _autoReconnect = true;

    public MavlinkTransport Transport { get => _transport; set => Set(ref _transport, value); }
    public string ListenAddress { get => _listenAddress; set => Set(ref _listenAddress, value); }
    public int Port { get => _port; set => Set(ref _port, Math.Clamp(value, 1, 65535)); }
    public string SerialPortName { get => _serialPortName; set => Set(ref _serialPortName, value?.Trim() ?? string.Empty); }
    public int BaudRate { get => _baudRate; set => Set(ref _baudRate, Math.Clamp(value, 1200, 3_000_000)); }

    /// <summary>0 bütün sistemleri kabul eder; 1..255 belirli MAVLink system id'yi filtreler.</summary>
    public int ExpectedSystemId { get => _expectedSystemId; set => Set(ref _expectedSystemId, Math.Clamp(value, 0, 255)); }

    public double StaleAfterSeconds
    {
        get => _staleAfterSeconds;
        set => Set(ref _staleAfterSeconds, Math.Clamp(value, 0.5, 30));
    }

    /// <summary>Kurulmuş seri MAVLink hattı koparsa aynı COM portunu yeniden açmayı dener.</summary>
    public bool AutoReconnect { get => _autoReconnect; set => Set(ref _autoReconnect, value); }

    public string ConnectionDescription => Transport == MavlinkTransport.Serial
        ? $"Seri {SerialPortName} @ {BaudRate} baud (8N1)"
        : $"UDP {ListenAddress}:{Port}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
