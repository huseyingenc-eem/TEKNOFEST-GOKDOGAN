using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// VİDEO sekmesi — RTSP URL + profil preset.
/// </summary>
public partial class VideoSettingsViewModel : OptionsBackedViewModel<VideoOptions>
{
    public VideoSettingsViewModel() { }

    public VideoSettingsViewModel(VideoOptions options) : base(options)
    {
        _rtspUrl = options.RtspUrl;
        _preset = options.Preset;
        _useTcp = options.UseTcp;
        _networkCachingMs = options.NetworkCachingMs;
    }

    [ObservableProperty] private string _rtspUrl = "rtsp://192.168.1.100:554/stream";
    [ObservableProperty] private string _preset = "1080p/30";
    [ObservableProperty] private bool _useTcp;
    [ObservableProperty] private int _networkCachingMs = 150;

    partial void OnRtspUrlChanged(string value) => PushToOptions(o => o.RtspUrl = value);
    partial void OnPresetChanged(string value) => PushToOptions(o => o.Preset = value);
    partial void OnUseTcpChanged(bool value) => PushToOptions(o => o.UseTcp = value);
    partial void OnNetworkCachingMsChanged(int value) => PushToOptions(o => o.NetworkCachingMs = value);
}
