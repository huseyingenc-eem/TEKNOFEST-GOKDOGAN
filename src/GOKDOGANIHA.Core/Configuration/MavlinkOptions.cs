using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Pixhawk/SITL MAVLink UDP dinleyici ayarları. İlk sürüm salt-okunur telemetri
/// alır; fiziksel uçuş komutları ayrı ve güvenli bir adaptör gelene kadar kapalıdır.
/// </summary>
public sealed class MavlinkOptions : INotifyPropertyChanged
{
    private string _listenAddress = "0.0.0.0";
    private int _port = 14550;
    private int _expectedSystemId;
    private double _staleAfterSeconds = 3;

    public string ListenAddress { get => _listenAddress; set => Set(ref _listenAddress, value); }
    public int Port { get => _port; set => Set(ref _port, Math.Clamp(value, 1, 65535)); }

    /// <summary>0 bütün sistemleri kabul eder; 1..255 belirli MAVLink system id'yi filtreler.</summary>
    public int ExpectedSystemId { get => _expectedSystemId; set => Set(ref _expectedSystemId, Math.Clamp(value, 0, 255)); }

    public double StaleAfterSeconds
    {
        get => _staleAfterSeconds;
        set => Set(ref _staleAfterSeconds, Math.Clamp(value, 0.5, 30));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
