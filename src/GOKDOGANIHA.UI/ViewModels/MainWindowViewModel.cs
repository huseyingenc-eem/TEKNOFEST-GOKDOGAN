using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.UI;

namespace GOKDOGANIHA.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    // Design-time: parameterless Settings (no service wiring).
    // Runtime: Settings gets the live App.ServerOptions + GameServer, so
    // every setter change propagates to the actual service layer.
    public MainWindowViewModel()
        : this(new MapViewModel(), CreateRuntimeSettings()) { }

    public MainWindowViewModel(MapViewModel mapVm)
        : this(mapVm, new SettingsViewModel()) { }

    public MainWindowViewModel(MapViewModel mapVm, SettingsViewModel settings)
    {
        MapVm = mapVm;
        Settings = settings;
    }

    public MainWindowViewModel(TelemetryPollService telemetryPoll, HssPollService hssPoll)
        : this(new MapViewModel(telemetryPoll, hssPoll), CreateRuntimeSettings()) { }

    // Factory for the runtime-wired SettingsViewModel. Falls back to a plain
    // (unwired) VM if App statics aren't bootstrapped — happens in unit tests
    // and sometimes in the XAML designer.
    private static SettingsViewModel CreateRuntimeSettings()
        => App.ServerOptions is null
            ? new SettingsViewModel()
            : new SettingsViewModel(App.ServerOptions, App.GameServer);

    public MapViewModel MapVm { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty] private string _serverTime = "00:00:00.000";
    [ObservableProperty] private string _flightMode = "MANUAL";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isAutonomous;
    [ObservableProperty] private bool _isLocked;

    [ObservableProperty] private double _airspeed;
    [ObservableProperty] private double _altitude;
    [ObservableProperty] private double _heading;
    [ObservableProperty] private double _pitch;
    [ObservableProperty] private double _roll;
    [ObservableProperty] private int _battery = 100;
    [ObservableProperty] private double _latitude;
    [ObservableProperty] private double _longitude;

    [ObservableProperty] private int _linkRssi;
    [ObservableProperty] private int _serverLatencyMs;

    // Target (shown in camera fullscreen when IsLocked)
    [ObservableProperty] private string _targetId = "—";
    [ObservableProperty] private double _targetRange;
    [ObservableProperty] private int _targetConfidence;

    // Camera state
    [ObservableProperty] private double _cameraZoom = 1.0;
    [ObservableProperty] private string _recordingTime = "00:00";

    // Overlay visibility (mutually exclusive — opening one closes the other)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyOverlayOpen))]
    private bool _isCameraFullscreen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyOverlayOpen))]
    private bool _isSettingsOpen;

    public bool IsAnyOverlayOpen => IsCameraFullscreen || IsSettingsOpen;

    partial void OnIsCameraFullscreenChanged(bool value)
    {
        if (value) IsSettingsOpen = false;
    }

    partial void OnIsSettingsOpenChanged(bool value)
    {
        if (value) IsCameraFullscreen = false;
    }

    [RelayCommand]
    private void ExpandCamera() => IsCameraFullscreen = true;

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void CloseActiveOverlay()
    {
        IsCameraFullscreen = false;
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void ToggleLock() => IsLocked = !IsLocked;

    [RelayCommand]
    private void CameraZoomIn() => CameraZoom = Math.Min(20.0, CameraZoom + 0.5);

    [RelayCommand]
    private void CameraZoomOut() => CameraZoom = Math.Max(1.0, CameraZoom - 0.5);

    [RelayCommand]
    private void ToggleCameraBand() { /* EO / IR toggle — bağlanacak */ }

    [RelayCommand]
    private void CameraSnapshot() { /* snapshot — bağlanacak */ }
}
