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
    private bool _useSimulator;

    public double Hz { get => _hz; set => Set(ref _hz, value); }
    public bool AutoReconnect { get => _autoReconnect; set => Set(ref _autoReconnect, value); }

    /// <summary>
    /// Gerçek MAVLink kaynağı yoksa, <see cref="Services.Telemetry.SimulatedFlightSource"/>
    /// "İHA'dan paket geliyormuş" gibi FlightState'i besler. Default: kapalı — yarışma /
    /// üretim modunda telemetri sıfır kalır, gerçek MAVLink adapter beslemediği sürece
    /// hiçbir uydurma değer ekrana çıkmaz. Settings UI'dan açılabilir.
    /// </summary>
    public bool UseSimulator { get => _useSimulator; set => Set(ref _useSimulator, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
