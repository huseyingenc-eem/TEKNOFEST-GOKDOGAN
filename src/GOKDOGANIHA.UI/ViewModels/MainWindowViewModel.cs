using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.UI;

namespace GOKDOGANIHA.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly FlightState? _flightState;
    private readonly DispatcherTimer? _clockTimer;

    // Design-time / parameterless: SettingsViewModel unwired (no service push).
    // Runtime: uses ApplicationOptions + GameServer from App composition root.
    public MainWindowViewModel()
        : this(new MapViewModel(), CreateRuntimeSettings()) { }

    public MainWindowViewModel(MapViewModel mapVm)
        : this(mapVm, new SettingsViewModel()) { }

    public MainWindowViewModel(MapViewModel mapVm, SettingsViewModel settings)
    {
        MapVm = mapVm;
        Settings = settings;

        // Runtime'da App.FlightState'e köprü kur — design-time'da Application yok.
        if (Application.Current is App)
        {
            _flightState = App.FlightState;
            _flightState.PropertyChanged += OnFlightStateChanged;
            SyncFromFlightState();

            // Sunucu saatini 100ms tick ile UI'a yansıt (ms hassasiyet — şartname zorunluluğu).
            _clockTimer = new DispatcherTimer(DispatcherPriority.DataBind)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();
            OnClockTick(this, EventArgs.Empty);
        }
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        var sc = App.ServerClock;
        var now = sc?.Now ?? DateTime.UtcNow;
        // Şartname formatı: gün-saat:dakika:saniye.ms
        ServerTime = now.ToString("dd-HH:mm:ss.fff");

        var le = App.LockEngine;
        if (le is not null)
        {
            LockState = le.State;
            LockProgressSeconds = le.LockProgressSeconds;
            LockTargetId = le.CurrentTargetId;
            LockRequiredSeconds = App.AppOptions.Autonomy.LockRequiredSeconds;
        }
    }

    public MainWindowViewModel(TelemetryPollService telemetryPoll, HssPollService hssPoll)
        : this(new MapViewModel(telemetryPoll, hssPoll), CreateRuntimeSettings()) { }

    // SettingsFactory üzerinden — statik App coupling yalnızca buraya sıkıştı.
    // Test'ler doğrudan SettingsViewModel constructor'ı ile ilerleyebilir.
    private static SettingsViewModel CreateRuntimeSettings()
        => App.SettingsFactory?.Create() ?? new SettingsViewModel();

    public MapViewModel MapVm { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty] private string _serverTime = "00:00:00.000";
    [ObservableProperty] private string _flightMode = "MANUAL";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isAutonomous;
    [ObservableProperty] private bool _isLocked;
    [ObservableProperty] private bool _isArmed;

    [ObservableProperty] private double _airspeed;
    [ObservableProperty] private double _groundSpeed;
    [ObservableProperty] private double _verticalSpeed;
    [ObservableProperty] private double _altitude;
    [ObservableProperty] private double _heading;
    [ObservableProperty] private double _pitch;
    [ObservableProperty] private double _roll;
    [ObservableProperty] private int _battery = 100;
    [ObservableProperty] private double _latitude;
    [ObservableProperty] private double _longitude;
    [ObservableProperty] private double _wpDistance;
    [ObservableProperty] private string _gpsFixDisplay = "—";
    [ObservableProperty] private int _satelliteCount;
    [ObservableProperty] private int? _targetTeamNumber;
    [ObservableProperty] private double _signalRssi;

    [ObservableProperty] private int _linkRssi;
    [ObservableProperty] private int _serverLatencyMs;
    [ObservableProperty] private double _telemetryHzActual;
    [ObservableProperty] private string _lastPacketStatus = "—";
    [ObservableProperty] private DateTime? _lastPacketTimestamp;

    // Lock state — KilitlenmeDenetim'den beslenir
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLockingActive))]
    private LockState _lockState = LockState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LockProgressText))]
    private double _lockProgressSeconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LockProgressText))]
    private double _lockRequiredSeconds = 4.0;
    [ObservableProperty] private int? _lockTargetId;
    [ObservableProperty] private string _lastLockAck = "—";

    // Kamera koordinatları (640x480 native varsayılan; FlightState.TargetCenter*'tan beslenir)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetBoxLeft), nameof(HasTargetBox))]
    private int _targetCenterX;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetBoxTop), nameof(TargetLabelTop), nameof(HasTargetBox))]
    private int _targetCenterY;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetBoxLeft), nameof(HasTargetBox))]
    private int _targetWidth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetBoxTop), nameof(TargetLabelTop), nameof(HasTargetBox))]
    private int _targetHeight;

    public double TargetBoxLeft => TargetCenterX - TargetWidth / 2.0;
    public double TargetBoxTop => TargetCenterY - TargetHeight / 2.0;
    public double TargetLabelTop => Math.Max(0, TargetBoxTop - 18);
    public bool HasTargetBox => TargetWidth > 0 && TargetHeight > 0;
    public string LockProgressText => $"{LockProgressSeconds:F1}s / {LockRequiredSeconds:F1}s";
    public bool IsLockingActive => LockState is LockState.Locking or LockState.Locked;

    // Target (shown in camera fullscreen when IsLocked)
    [ObservableProperty] private string _targetId = "—";
    [ObservableProperty] private double _targetRange;
    [ObservableProperty] private int _targetConfidence;

    // Camera state
    [ObservableProperty] private double _cameraZoom = 1.0;
    [ObservableProperty] private string _recordingTime = "00:00";

    // HUD overlay state — TelemetryPanel kompakt başlar, kullanıcı tıklayınca expand olur.
    // Settings persistence Faz 8'de gelince bu state diske yazılır.
    [ObservableProperty] private bool _isTelemetryExpanded;

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

    [RelayCommand] private void ExpandCamera() => IsCameraFullscreen = true;
    [RelayCommand] private void OpenSettings() => IsSettingsOpen = true;
    [RelayCommand]
    private void CloseActiveOverlay()
    {
        IsCameraFullscreen = false;
        IsSettingsOpen = false;
    }
    [RelayCommand] private void ToggleLock() => IsLocked = !IsLocked;
    [RelayCommand] private void CameraZoomIn() => CameraZoom = Math.Min(20.0, CameraZoom + 0.5);
    [RelayCommand] private void CameraZoomOut() => CameraZoom = Math.Max(1.0, CameraZoom - 0.5);
    [RelayCommand] private void ToggleCameraBand() { /* EO / IR — bağlanacak */ }
    [RelayCommand] private void CameraSnapshot() { /* snapshot — bağlanacak */ }

    // ----- FlightState bridge --------------------------------------------------
    private void OnFlightStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        // FlightState arka thread'lerden push edebilir; UI thread'inde yansıt.
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null) { ApplyFromFlightState(e.PropertyName); return; }
        if (dispatcher.CheckAccess()) ApplyFromFlightState(e.PropertyName);
        else dispatcher.BeginInvoke(new Action(() => ApplyFromFlightState(e.PropertyName)));
    }

    private void ApplyFromFlightState(string? propertyName)
    {
        if (_flightState is null) return;
        switch (propertyName)
        {
            case nameof(FlightState.Latitude):       Latitude        = _flightState.Latitude; break;
            case nameof(FlightState.Longitude):      Longitude       = _flightState.Longitude; break;
            case nameof(FlightState.Altitude):       Altitude        = _flightState.Altitude; break;
            case nameof(FlightState.Pitch):          Pitch           = _flightState.Pitch; break;
            case nameof(FlightState.Heading):        Heading         = _flightState.Heading; break;
            case nameof(FlightState.Roll):           Roll            = _flightState.Roll; break;
            case nameof(FlightState.GroundSpeed):    GroundSpeed     = _flightState.GroundSpeed; break;
            case nameof(FlightState.Airspeed):       Airspeed        = _flightState.Airspeed; break;
            case nameof(FlightState.VerticalSpeed):  VerticalSpeed   = _flightState.VerticalSpeed; break;
            case nameof(FlightState.WpDistance):     WpDistance      = _flightState.WpDistance; break;
            case nameof(FlightState.BatteryPercent): Battery         = _flightState.BatteryPercent; break;
            case nameof(FlightState.IsAutonomous):   IsAutonomous    = _flightState.IsAutonomous; break;
            case nameof(FlightState.IsLocked):       IsLocked        = _flightState.IsLocked; break;
            case nameof(FlightState.IsArmed):        IsArmed         = _flightState.IsArmed; break;
            case nameof(FlightState.Mode):           FlightMode      = _flightState.Mode.ToString().ToUpperInvariant(); break;
            case nameof(FlightState.GpsFix):
            case nameof(FlightState.SatelliteCount):
                GpsFixDisplay = $"{FormatGpsFix(_flightState.GpsFix)} ({_flightState.SatelliteCount} sat)";
                SatelliteCount = _flightState.SatelliteCount;
                break;
            case nameof(FlightState.TargetTeamNumber): TargetTeamNumber = _flightState.TargetTeamNumber; break;
            case nameof(FlightState.SignalRssi):
                SignalRssi = _flightState.SignalRssi;
                LinkRssi = (int)Math.Round(_flightState.SignalRssi);
                break;
            case nameof(FlightState.TargetCenterX): TargetCenterX = _flightState.TargetCenterX; break;
            case nameof(FlightState.TargetCenterY): TargetCenterY = _flightState.TargetCenterY; break;
            case nameof(FlightState.TargetWidth):   TargetWidth   = _flightState.TargetWidth; break;
            case nameof(FlightState.TargetHeight):  TargetHeight  = _flightState.TargetHeight; break;
            case null: SyncFromFlightState(); break;
        }
    }

    private void SyncFromFlightState()
    {
        if (_flightState is null) return;
        Latitude       = _flightState.Latitude;
        Longitude      = _flightState.Longitude;
        Altitude       = _flightState.Altitude;
        Pitch          = _flightState.Pitch;
        Heading        = _flightState.Heading;
        Roll           = _flightState.Roll;
        GroundSpeed    = _flightState.GroundSpeed;
        Airspeed       = _flightState.Airspeed;
        VerticalSpeed  = _flightState.VerticalSpeed;
        WpDistance     = _flightState.WpDistance;
        Battery        = _flightState.BatteryPercent;
        IsAutonomous   = _flightState.IsAutonomous;
        IsLocked       = _flightState.IsLocked;
        IsArmed        = _flightState.IsArmed;
        FlightMode     = _flightState.Mode.ToString().ToUpperInvariant();
        SatelliteCount = _flightState.SatelliteCount;
        GpsFixDisplay  = $"{FormatGpsFix(_flightState.GpsFix)} ({_flightState.SatelliteCount} sat)";
        TargetTeamNumber = _flightState.TargetTeamNumber;
        SignalRssi     = _flightState.SignalRssi;
        LinkRssi       = (int)Math.Round(_flightState.SignalRssi);
        TargetCenterX  = _flightState.TargetCenterX;
        TargetCenterY  = _flightState.TargetCenterY;
        TargetWidth    = _flightState.TargetWidth;
        TargetHeight   = _flightState.TargetHeight;
    }

    private static string FormatGpsFix(GpsFix fix) => fix switch
    {
        GpsFix.None  => "—",
        GpsFix.NoFix => "No Fix",
        GpsFix.Fix2D => "2D Fix",
        GpsFix.Fix3D => "3D Fix",
        GpsFix.Dgps  => "DGPS",
        GpsFix.Rtk   => "RTK",
        _ => "—"
    };
}
