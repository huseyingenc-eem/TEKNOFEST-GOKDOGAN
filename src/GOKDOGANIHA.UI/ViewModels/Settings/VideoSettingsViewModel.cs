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
    }

    [ObservableProperty] private string _rtspUrl = "rtsp://192.168.1.100:554/stream";
    [ObservableProperty] private string _preset = "1080p/30";

    partial void OnRtspUrlChanged(string value) => PushToOptions(o => o.RtspUrl = value);
    partial void OnPresetChanged(string value) => PushToOptions(o => o.Preset = value);
}
