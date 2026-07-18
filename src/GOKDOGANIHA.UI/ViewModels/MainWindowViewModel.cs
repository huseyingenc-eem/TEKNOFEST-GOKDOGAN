using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.UI;
// VM'de string property `FlightMode` jeneratör tarafından üretildiği için
// enum'a alias veriyoruz — yoksa `FlightMode.Auto` ifadesi string property'sinin
// üyesini arar (bulamaz).
using FlightModeEnum = GOKDOGANIHA.Core.Models.FlightMode;

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

            // Simülatör açıkken yarışma sahasının yakınında mock QR target üret —
            // sunucuya bağlanmadan kamikaze görevi test edilebilsin. Sim toggle'ı
            // değişince QrTarget set/null olur (gerçek modda sunucudan gelecek).
            App.AppOptions.Telemetry.PropertyChanged += OnTelemetryOptionsChanged;
            ApplyMockQrTargetIfSim();
        }
    }

    private void OnTelemetryOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Tam qualified — `Core.Configuration` ifadesi GOKDOGANIHA.UI.Core namespace
        // içinde aranıp bulunamıyordu (UI.Core mevcut). nameof string döner, runtime
        // etkisi yok ama compile-time symbol resolution gerek.
        if (e.PropertyName == nameof(GOKDOGANIHA.Core.Configuration.TelemetryOptions.UseSimulator))
            ApplyMockQrTargetIfSim();
    }

    private void ApplyMockQrTargetIfSim()
    {
        if (MapVm is null) return;
        if (App.AppOptions.Telemetry.UseSimulator)
        {
            // Sim merkezi (Ankara) yakın — yarışma sahasında plausible bir hedef.
            MapVm.QrTarget = new QrKoordinat(39.9230, 32.8560);
        }
        else
        {
            // Gerçek mod: sunucudan QR koordinatı gelene kadar null.
            MapVm.QrTarget = null;
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

        // Sunucu haberleşme metrikleri — telemetri kesintisinde Hz'in donmaması için Refresh().
        var hz = App.HzMeter;
        if (hz is not null)
        {
            hz.Refresh();
            TelemetryHzMeasured = hz.CurrentHz;
            TelemetryStatus = hz.LastStatus;
            ServerLatencyLiveMs = hz.LastLatencyMs;
            HasTelemetrySignal = hz.LastReceivedUtc.HasValue;
            // ServerLatencyMs ana property'si (TopBar pill için) de canlı bağlansın
            ServerLatencyMs = (int)Math.Round(hz.LastLatencyMs);
        }

        // Güvenlik durumları
        if (App.BoundaryProximity is { } bp)
        {
            DistanceToBoundaryEdge = bp.DistanceToEdgeMeters;
            IsInsideBoundary = bp.IsInside;
        }
        if (App.HssProximity is { } hp)
        {
            HssViolationCount = hp.ActiveViolationCount;
            HssViolationSeconds = hp.ActiveViolationDuration.TotalSeconds;
        }
        if (App.CommLatency is { } cl)
            CommDropoutCount = cl.DropoutCount;
        if (App.Failsafe is { } fs)
            IsFailsafeActive = fs.IsGcsLost;
        if (App.ManualTransitions is { } mt)
            ManualTransitionCount = mt.Count;

        // Kamikaze FSM tick — telemetri akışını state machine'e besle.
        if (App.Kamikaze is { } kam)
        {
            if (_flightState is not null) kam.Tick(_flightState);
            KamikazePhase = kam.Phase;
            KamikazeStatus = kam.StatusMessage;
            KamikazeQrText = kam.QrText;
            KamikazeAttempt = kam.AttemptCount;

            // Geçen süre — şartname m. 6.2 kamikazeBaslangicZamani / kamikazeBitisZamani
            // farkı sunucuya gönderiliyor; UI'da pilota canlı gösterim.
            if (kam.MissionStartUtc is { } start && kam.IsActive)
            {
                var span = DateTime.UtcNow - start;
                KamikazeElapsed = $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
            }
            else if (!kam.IsActive && kam.Phase is KamikazePhase.Idle)
            {
                KamikazeElapsed = "—";
            }

            // Aktif olma durumu değişince fullscreen overlay'i otomatik aç/kapa.
            // Görev başlayınca pilotun dikkati buraya çekilir; biter bitmez normal moda dönülür.
            var nowActive = kam.IsActive;
            if (nowActive != IsKamikazeActive)
            {
                IsKamikazeActive = nowActive;
                IsKamikazeFullscreen = nowActive;
            }
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

    // Kamikaze FSM bindings (Faz 7)
    [ObservableProperty] private KamikazePhase _kamikazePhase = KamikazePhase.Idle;
    [ObservableProperty] private string _kamikazeStatus = "Görev pasif";
    [ObservableProperty] private string _kamikazeQrText = "";
    [ObservableProperty] private int _kamikazeAttempt;
    [ObservableProperty] private bool _isKamikazeActive;
    [ObservableProperty] private string _kamikazeElapsed = "—";

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

    // Sidebar panellerinin canlı veri property'leri — 100ms tick'te servislerden çekilir.
    [ObservableProperty] private double _telemetryHzMeasured;
    [ObservableProperty] private string _telemetryStatus = "—";
    [ObservableProperty] private double _serverLatencyLiveMs;
    [ObservableProperty] private bool _hasTelemetrySignal;

    // Safety panel
    [ObservableProperty] private bool _isInsideBoundary = true;
    [ObservableProperty] private double _distanceToBoundaryEdge;
    [ObservableProperty] private int _hssViolationCount;
    [ObservableProperty] private double _hssViolationSeconds;
    [ObservableProperty] private int _manualTransitionCount;
    [ObservableProperty] private bool _isFailsafeActive;
    [ObservableProperty] private int _commDropoutCount;

    // Overlay visibility (mutually exclusive — opening one closes the other)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyOverlayOpen))]
    private bool _isCameraFullscreen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyOverlayOpen))]
    private bool _isSettingsOpen;

    /// <summary>
    /// Kamikaze tam ekran taktik görünüm. KamikazeFsm aktif olunca otomatik açılır,
    /// abort/tamam/hata olunca otomatik kapanır. Camera/Settings overlay'lerinden
    /// önceliklidir — görev anında dikkat dağılmaz.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyOverlayOpen))]
    private bool _isKamikazeFullscreen;

    public bool IsAnyOverlayOpen => IsCameraFullscreen || IsSettingsOpen || IsKamikazeFullscreen;

    partial void OnIsCameraFullscreenChanged(bool value)
    {
        if (value) { IsSettingsOpen = false; IsKamikazeFullscreen = false; }
    }

    partial void OnIsSettingsOpenChanged(bool value)
    {
        if (value) { IsCameraFullscreen = false; IsKamikazeFullscreen = false; }
    }

    partial void OnIsKamikazeFullscreenChanged(bool value)
    {
        if (value) { IsCameraFullscreen = false; IsSettingsOpen = false; }
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

    // ----- Flight commands (CommandsPanel buttons → IFlightCommandSink) --------
    // Şu an Null sink (AlertBus'a Info publish), Faz 9 MAVLink ile gerçek komut.
    [RelayCommand]
    private async Task Arm()
    {
        if (IsArmed)
        {
            // Safety-critical: uçakta DISARM = motor durur. Confirmation iste.
            var ok = await App.Dialogs.ConfirmAsync(
                title: "DISARM ONAYI",
                message: "Motorlar durdurulacak. Uçak havadaysa düşecektir.\n\nDevam edilsin mi?",
                yesText: "Evet, DISARM",
                noText: "İptal");
            if (ok) App.Commands?.Disarm();
        }
        else
        {
            App.Commands?.Arm();
        }
    }

    /// <summary>Mod butonu: CommandParameter olarak "MANUAL"/"AUTO"/... gelir.</summary>
    [RelayCommand]
    private void SetMode(string? mode)
    {
        if (string.IsNullOrEmpty(mode)) return;
        if (Enum.TryParse<FlightModeEnum>(mode, ignoreCase: true, out var fm))
            App.Commands?.SetMode(fm);
    }

    [RelayCommand] private void Rtl()    => App.Commands?.Rtl();
    [RelayCommand] private void Land()   => App.Commands?.Land();
    [RelayCommand] private void Loiter() => App.Commands?.Loiter();

    // ----- Kamikaze (Faz 7) -----
    /// <summary>
    /// Kamikaze görevini başlat. QR koordinatı sırası:
    /// 1) MapVm.QrTarget (sunucudan gelir veya sim modunda mock'lanır)
    /// 2) Yoksa: fallback mock koordinat üretilir (görev test edilebilsin diye)
    ///    + Olay Günlüğü'ne uyarı yazılır ki kullanıcı mock kullanıldığını bilsin.
    /// </summary>
    [RelayCommand]
    private void StartKamikaze()
    {
        var target = MapVm?.QrTarget;
        if (target is null)
        {
            target = new QrKoordinat(39.9230, 32.8560);
            if (MapVm is not null) MapVm.QrTarget = target;
            App.AlertBus.Publish(Alert.Create(
                kind: "kamikaze.start-mock",
                level: AlertLevel.Warn,
                title: "KAMİKAZE — MOCK QR",
                message: "Sunucu QR koordinatı yok; test için sahte koordinat kullanılıyor.",
                timeUtc: App.Clock.UtcNow));
        }
        App.Kamikaze?.StartMission(target.Enlem, target.Boylam);
    }

    [RelayCommand] private void AbortKamikaze() => App.Kamikaze?.Abort("Kullanıcı iptali");

    /// <summary>Settings/Kamikaze panel debug butonu — gerçek QR detection yokken FSM'i ileri itmek için.</summary>
    [RelayCommand]
    private void SimulateQrRead() => App.Kamikaze?.ApplyQrRead("MOCK-QR-12345");

    /// <summary>OTONOM TAKİP toggle — şu an local flag + alert. MAVLink ile mode change'a bağlanır.</summary>
    [RelayCommand]
    private void ToggleAutonomous()
    {
        var next = !IsAutonomous;
        // MAVLink hazır olunca: Auto ↔ Guided gibi gerçek mod komutu.
        App.Commands?.SetMode(next ? FlightModeEnum.Auto : FlightModeEnum.Manual);
    }

    /// <summary>HEDEF SEÇ — Faz 7'de KilitlenmeDenetim ile entegre. Şimdilik feedback alert.</summary>
    [RelayCommand]
    private void SelectTarget()
    {
        App.AlertBus.Publish(Alert.Create(
            kind: "command.target-select",
            level: AlertLevel.Info,
            title: "HEDEF SEÇ",
            message: "Hedef seçim diyaloğu — Faz 7 KilitlenmeDenetim entegrasyonu",
            timeUtc: App.Clock.UtcNow));
    }

    /// <summary>WAYPOINT GİT — şimdilik kullanıcı haritaya tıklayıp ilk waypoint'e gönderir.</summary>
    [RelayCommand]
    private void GotoWaypoint()
    {
        var first = MapVm?.Waypoints.Count > 0 ? MapVm.Waypoints[0] : null;
        if (first is null)
        {
            App.AlertBus.Publish(Alert.Create(
                kind: "command.goto-empty",
                level: AlertLevel.Warn,
                title: "WAYPOINT GİT",
                message: "Henüz tanımlı waypoint yok — önce harita üzerinden ekleyin",
                timeUtc: App.Clock.UtcNow));
            return;
        }
        App.Commands?.GotoWaypoint(first.Latitude, first.Longitude, first.AltitudeMeters);
    }

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
            case nameof(FlightState.Latitude):
            case nameof(FlightState.Longitude):
            case nameof(FlightState.Heading):
                // Lat/Lon/Heading değiştiğinde MapVm'i besle — own marker + trail.
                Latitude  = _flightState.Latitude;
                Longitude = _flightState.Longitude;
                Heading   = _flightState.Heading;
                MapVm?.SetOwnPosition(_flightState.Latitude, _flightState.Longitude, _flightState.Heading);
                break;
            case nameof(FlightState.Altitude):       Altitude        = _flightState.Altitude; break;
            case nameof(FlightState.Pitch):          Pitch           = _flightState.Pitch; break;
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
        MapVm?.SetOwnPosition(_flightState.Latitude, _flightState.Longitude, _flightState.Heading);
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
