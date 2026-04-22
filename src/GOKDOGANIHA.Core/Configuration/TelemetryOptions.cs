using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Telemetri servisi davranışını kontrol eden ayarlar. INPC → Hz değişince
/// TelemetryPollService interval'ı günceller. Şartname: max 2 Hz.
/// </summary>
public sealed class TelemetryOptions : INotifyPropertyChanged
{
    private double _hz = 1.0;
    private bool _autoReconnect = true;

    public double Hz { get => _hz; set => Set(ref _hz, value); }
    public bool AutoReconnect { get => _autoReconnect; set => Set(ref _autoReconnect, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
