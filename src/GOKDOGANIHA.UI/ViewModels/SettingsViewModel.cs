using CommunityToolkit.Mvvm.ComponentModel;

namespace GOKDOGANIHA.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    // Yarışma sunucusu
    [ObservableProperty] private string _serverBaseUrl = "http://127.0.0.25:5000";
    [ObservableProperty] private string _teamUsername = "gokdogan";
    [ObservableProperty] private string _teamPassword = string.Empty;

    // Telemetri
    [ObservableProperty] private double _telemetryHz = 1.0;
    [ObservableProperty] private bool _autoReconnect = true;

    // Video
    [ObservableProperty] private string _videoRtspUrl = "rtsp://192.168.1.100:554/stream";
    [ObservableProperty] private string _videoPreset = "1080p/30";

    // Harita
    [ObservableProperty] private string _mapTileProvider = "OpenStreetMap";
    [ObservableProperty] private bool _showGrid = true;
    [ObservableProperty] private bool _showBoundary = true;
    [ObservableProperty] private bool _showHssZones = true;

    // Uyarılar
    [ObservableProperty] private bool _enableAudioAlerts = true;
    [ObservableProperty] private bool _enableLockBeep = true;
    [ObservableProperty] private int _lowBatteryThreshold = 22;
}
