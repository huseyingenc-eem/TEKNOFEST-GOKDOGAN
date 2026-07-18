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
    private bool _useTcp;
    private int _networkCachingMs = 150;

    public string RtspUrl { get => _rtspUrl; set => Set(ref _rtspUrl, value); }
    public string Preset { get => _preset; set => Set(ref _preset, value); }

    /// <summary>
    /// RTSP taşıma katmanı. <c>false</c> = UDP/RTP (KTR 6.2 taahhüdü · düşük gecikme),
    /// <c>true</c> = TCP interleaved (paket kaybına dayanıklı ama daha yüksek gecikme).
    /// </summary>
    public bool UseTcp { get => _useTcp; set => Set(ref _useTcp, value); }

    /// <summary>Ağ jitter tamponu (ms). Düşük = az gecikme, yüksek = az takılma. 0–5000 aralığına clamp'lenir.</summary>
    public int NetworkCachingMs { get => _networkCachingMs; set => Set(ref _networkCachingMs, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
