using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Görev video akışı ayarları. INPC → VideoPanel URL değişince stream'i
/// re-init eder.
/// </summary>
public sealed class VideoOptions : INotifyPropertyChanged
{
    private string _rtspUrl = "rtsp://192.168.1.100:554/stream";
    private string _preset = "1080p/30";

    public string RtspUrl { get => _rtspUrl; set => Set(ref _rtspUrl, value); }
    public string Preset { get => _preset; set => Set(ref _preset, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
