using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Autonomy;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.UI.ViewModels;
using GpsHealthLevel = GOKDOGANIHA.Core.Models.GpsHealth;

namespace GOKDOGANIHA.UI.ViewModels.Flight;

public partial class FlightTelemetryViewModel : ObservableObject, IDisposable
{
    private readonly FlightState? _flightState;
    private readonly FlightBackendCoordinator? _backend;
    private readonly KilitlenmeDenetim? _lockEngine;
    private readonly MavlinkOptions _mavlinkOptions;
    private readonly AutonomyOptions _autonomyOptions;

    public FlightTelemetryViewModel(MapViewModel mapVm)
        : this(mapVm, null, null, null, new MavlinkOptions(), new AutonomyOptions()) { }

    internal FlightTelemetryViewModel(
        MapViewModel mapVm,
        FlightState? flightState,
        FlightBackendCoordinator? backend,
        KilitlenmeDenetim? lockEngine,
        MavlinkOptions mavlinkOptions,
        AutonomyOptions autonomyOptions)
    {
        Map = mapVm;
        _flightState = flightState;
        _backend = backend;
        _lockEngine = lockEngine;
        _mavlinkOptions = mavlinkOptions;
        _autonomyOptions = autonomyOptions;

        if (_flightState is not null)
        {
            _flightState.PropertyChanged += OnFlightStateChanged;
            SyncFromFlightState();
        }

        if (_backend is not null)
            _backend.PropertyChanged += OnBackendChanged;

        SyncBackendPresentation();
    }

    public MapViewModel Map { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FlightModeDisplay))]
    private string _flightMode = "MANUAL";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAutonomousDisplay))]
    private bool _isAutonomous;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLockedDisplay))]
    private bool _isLocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsArmedDisplay))]
    private bool _isArmed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryAvailabilityMessage))]
    private string _backendStatusText = "Veri kaynağı başlatılıyor";

    [ObservableProperty] private string _dataModeText = "CANLI";
    [ObservableProperty] private bool _isSimulationMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVehicleDataUnavailable))]
    [NotifyPropertyChangedFor(nameof(TelemetryAvailabilityMessage))]
    [NotifyPropertyChangedFor(nameof(FlightModeDisplay))]
    [NotifyPropertyChangedFor(nameof(BatteryDisplay))]
    [NotifyPropertyChangedFor(nameof(BatteryProgressValue))]
    [NotifyPropertyChangedFor(nameof(AltitudeDisplay))]
    [NotifyPropertyChangedFor(nameof(PitchDisplay))]
    [NotifyPropertyChangedFor(nameof(VerticalSpeedDisplay))]
    [NotifyPropertyChangedFor(nameof(HasTargetBox))]
    [NotifyPropertyChangedFor(nameof(IsLockingActive))]
    [NotifyPropertyChangedFor(nameof(IsArmedDisplay))]
    [NotifyPropertyChangedFor(nameof(IsAutonomousDisplay))]
    [NotifyPropertyChangedFor(nameof(IsLockedDisplay))]
    private bool _isVehicleDataValid;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVehicleDataUnavailable))]
    [NotifyPropertyChangedFor(nameof(TelemetryAvailabilityMessage))]
    [NotifyPropertyChangedFor(nameof(FlightModeDisplay))]
    [NotifyPropertyChangedFor(nameof(BatteryDisplay))]
    [NotifyPropertyChangedFor(nameof(BatteryProgressValue))]
    [NotifyPropertyChangedFor(nameof(AltitudeDisplay))]
    [NotifyPropertyChangedFor(nameof(PitchDisplay))]
    [NotifyPropertyChangedFor(nameof(VerticalSpeedDisplay))]
    private bool _hasVehicleTelemetrySnapshot;

    [ObservableProperty] private double _vehicleDataAgeSeconds;
    [ObservableProperty] private double _airspeed;
    [ObservableProperty] private double _groundSpeed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VerticalSpeedDisplay))]
    private double _verticalSpeed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AltitudeDisplay))]
    private double _altitude;

    [ObservableProperty] private double _heading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PitchDisplay))]
    private double _pitch;

    [ObservableProperty] private double _roll;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BatteryDisplay))]
    [NotifyPropertyChangedFor(nameof(BatteryProgressValue))]
    private int _battery = 100;

    [ObservableProperty] private double _latitude;
    [ObservableProperty] private double _longitude;
    [ObservableProperty] private double _wpDistance;
    [ObservableProperty] private string _gpsFixDisplay = "—";
    [ObservableProperty] private int _satelliteCount;
    [ObservableProperty] private double? _gpsHdop;
    [ObservableProperty] private GpsHealthLevel _gpsHealth = GpsHealthLevel.Unavailable;
    [ObservableProperty] private string _gpsHealthDisplay = "VERİ YOK";
    [ObservableProperty] private int? _targetTeamNumber;
    [ObservableProperty] private double _signalRssi;
    [ObservableProperty] private int _linkRssi;

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

    [ObservableProperty] private string _targetId = "—";
    [ObservableProperty] private double _targetRange;
    [ObservableProperty] private int _targetConfidence;

    public double TargetBoxLeft => TargetCenterX - TargetWidth / 2.0;
    public double TargetBoxTop => TargetCenterY - TargetHeight / 2.0;
    public double TargetLabelTop => Math.Max(0, TargetBoxTop - 18);
    public bool HasTargetBox => IsVehicleDataValid && TargetWidth > 0 && TargetHeight > 0;
    public string LockProgressText => $"{LockProgressSeconds:F1}s / {LockRequiredSeconds:F1}s";
    public bool IsLockingActive => IsVehicleDataValid && LockState is LockState.Locking or LockState.Locked;
    public bool IsVehicleDataUnavailable => !HasVehicleTelemetrySnapshot;

    public string TelemetryAvailabilityMessage => IsVehicleDataValid
        ? string.Empty
        : BackendStatusText.Contains("BAĞLANTI KOPTU", StringComparison.OrdinalIgnoreCase)
            ? "BAĞLANTI KOPTU"
            : "TELEMETRİ BEKLENİYOR";

    public string FlightModeDisplay => HasVehicleTelemetrySnapshot ? FlightMode : "—";
    public string BatteryDisplay => HasVehicleTelemetrySnapshot ? $"{Battery}%" : "—";
    public double BatteryProgressValue => HasVehicleTelemetrySnapshot ? Battery : 0;
    public string AltitudeDisplay => HasVehicleTelemetrySnapshot ? $"{Altitude:0} m" : "—";
    public string PitchDisplay => HasVehicleTelemetrySnapshot ? $"{Pitch:0}°" : "—";
    public string VerticalSpeedDisplay => HasVehicleTelemetrySnapshot ? $"{VerticalSpeed:0}" : "—";
    public bool IsArmedDisplay => IsVehicleDataValid && IsArmed;
    public bool IsAutonomousDisplay => IsVehicleDataValid && IsAutonomous;
    public bool IsLockedDisplay => IsVehicleDataValid && IsLocked;

    public void Refresh(DateTime nowUtc)
    {
        SyncBackendPresentation();

        if (_lockEngine is not null)
        {
            LockState = _lockEngine.State;
            LockProgressSeconds = _lockEngine.LockProgressSeconds;
            LockTargetId = _lockEngine.CurrentTargetId;
            LockRequiredSeconds = _autonomyOptions.LockRequiredSeconds;
        }

        if (_flightState is null) return;

        IsVehicleDataValid = _flightState.IsFresh(
            nowUtc,
            TimeSpan.FromSeconds(_mavlinkOptions.StaleAfterSeconds));
        VehicleDataAgeSeconds = _flightState.LastUpdatedUtc is { } last
            ? Math.Max(0, (nowUtc - last).TotalSeconds)
            : 0;
        UpdateMapVehicleStatus();
    }

    public void Dispose()
    {
        if (_flightState is not null)
            _flightState.PropertyChanged -= OnFlightStateChanged;
        if (_backend is not null)
            _backend.PropertyChanged -= OnBackendChanged;
    }

    private void OnBackendChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(FlightBackendCoordinator.ActiveMode)
            or nameof(FlightBackendCoordinator.Status)
            or nameof(FlightBackendCoordinator.StatusMessage))) return;

        Dispatch(SyncBackendPresentation);
    }

    private void OnFlightStateChanged(object? sender, PropertyChangedEventArgs e)
        => Dispatch(() => ApplyFromFlightState(e.PropertyName));

    private static void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }

    private void ApplyFromFlightState(string? propertyName)
    {
        if (_flightState is null) return;

        switch (propertyName)
        {
            case nameof(FlightState.Latitude):
            case nameof(FlightState.Longitude):
            case nameof(FlightState.Heading):
                Latitude = _flightState.Latitude;
                Longitude = _flightState.Longitude;
                Heading = _flightState.Heading;
                break;
            case nameof(FlightState.Sequence):
                HasVehicleTelemetrySnapshot = _flightState.Sequence > 0;
                Latitude = _flightState.Latitude;
                Longitude = _flightState.Longitude;
                Heading = _flightState.Heading;
                break;
            case nameof(FlightState.NavigationSequence):
                Map.SetOwnPosition(
                    _flightState.Latitude,
                    _flightState.Longitude,
                    _flightState.Heading,
                    isValid: _flightState.IsDataValid,
                    groundTrackDeg: _flightState.GroundTrack,
                    groundSpeedMps: _flightState.GroundSpeed,
                    gpsHdop: _flightState.GpsHdop,
                    sampleUtc: _flightState.LastNavigationUpdatedUtc);
                break;
            case nameof(FlightState.Altitude): Altitude = _flightState.Altitude; break;
            case nameof(FlightState.Pitch): Pitch = _flightState.Pitch; break;
            case nameof(FlightState.Roll): Roll = _flightState.Roll; break;
            case nameof(FlightState.GroundSpeed): GroundSpeed = _flightState.GroundSpeed; break;
            case nameof(FlightState.Airspeed): Airspeed = _flightState.Airspeed; break;
            case nameof(FlightState.VerticalSpeed): VerticalSpeed = _flightState.VerticalSpeed; break;
            case nameof(FlightState.WpDistance): WpDistance = _flightState.WpDistance; break;
            case nameof(FlightState.BatteryPercent): Battery = _flightState.BatteryPercent; break;
            case nameof(FlightState.IsAutonomous): IsAutonomous = _flightState.IsAutonomous; break;
            case nameof(FlightState.IsLocked): IsLocked = _flightState.IsLocked; break;
            case nameof(FlightState.IsArmed): IsArmed = _flightState.IsArmed; break;
            case nameof(FlightState.Mode): FlightMode = _flightState.Mode.ToString().ToUpperInvariant(); break;
            case nameof(FlightState.GpsFix):
            case nameof(FlightState.SatelliteCount):
            case nameof(FlightState.GpsHdop):
                SatelliteCount = _flightState.SatelliteCount;
                GpsHdop = _flightState.GpsHdop;
                UpdateGpsPresentation();
                break;
            case nameof(FlightState.TargetTeamNumber): TargetTeamNumber = _flightState.TargetTeamNumber; break;
            case nameof(FlightState.SignalRssi):
                SignalRssi = _flightState.SignalRssi;
                LinkRssi = (int)Math.Round(_flightState.SignalRssi);
                break;
            case nameof(FlightState.TargetCenterX): TargetCenterX = _flightState.TargetCenterX; break;
            case nameof(FlightState.TargetCenterY): TargetCenterY = _flightState.TargetCenterY; break;
            case nameof(FlightState.TargetWidth): TargetWidth = _flightState.TargetWidth; break;
            case nameof(FlightState.TargetHeight): TargetHeight = _flightState.TargetHeight; break;
            case nameof(FlightState.IsDataValid):
            case nameof(FlightState.DataSource):
            case nameof(FlightState.LastUpdatedUtc):
                IsVehicleDataValid = _flightState.IsDataValid;
                UpdateGpsPresentation();
                UpdateMapVehicleStatus();
                if (!_flightState.IsDataValid) Map.SetOwnPosition(0, 0, 0, false);
                break;
            case null:
                SyncFromFlightState();
                break;
        }
    }

    private void SyncFromFlightState()
    {
        if (_flightState is null) return;

        HasVehicleTelemetrySnapshot = _flightState.Sequence > 0;
        IsVehicleDataValid = _flightState.IsDataValid;
        Latitude = _flightState.Latitude;
        Longitude = _flightState.Longitude;
        Altitude = _flightState.Altitude;
        Pitch = _flightState.Pitch;
        Heading = _flightState.Heading;
        Roll = _flightState.Roll;
        GroundSpeed = _flightState.GroundSpeed;
        Airspeed = _flightState.Airspeed;
        VerticalSpeed = _flightState.VerticalSpeed;
        WpDistance = _flightState.WpDistance;
        Battery = _flightState.BatteryPercent;
        IsAutonomous = _flightState.IsAutonomous;
        IsLocked = _flightState.IsLocked;
        IsArmed = _flightState.IsArmed;
        FlightMode = _flightState.Mode.ToString().ToUpperInvariant();
        SatelliteCount = _flightState.SatelliteCount;
        GpsHdop = _flightState.GpsHdop;
        TargetTeamNumber = _flightState.TargetTeamNumber;
        SignalRssi = _flightState.SignalRssi;
        LinkRssi = (int)Math.Round(_flightState.SignalRssi);
        TargetCenterX = _flightState.TargetCenterX;
        TargetCenterY = _flightState.TargetCenterY;
        TargetWidth = _flightState.TargetWidth;
        TargetHeight = _flightState.TargetHeight;

        Map.SetOwnPosition(
            _flightState.Latitude,
            _flightState.Longitude,
            _flightState.Heading,
            _flightState.IsDataValid,
            _flightState.GroundTrack,
            _flightState.GroundSpeed,
            _flightState.GpsHdop,
            _flightState.LastNavigationUpdatedUtc);
        UpdateGpsPresentation();
        UpdateMapVehicleStatus();
    }

    private void SyncBackendPresentation()
    {
        if (_backend is null) return;

        IsSimulationMode = _backend.IsSimulationActive;
        DataModeText = _backend.ActiveMode == FlightDataMode.Simulation ? "SİMÜLASYON" : "CANLI";
        BackendStatusText = _backend.StatusMessage;
        UpdateMapVehicleStatus();
    }

    private void UpdateMapVehicleStatus()
    {
        Map.SetVehicleStatus(
            IsVehicleDataValid,
            _flightState?.DataSource ?? "NONE",
            BackendStatusText,
            IsSimulationMode,
            _flightState?.LastUpdatedUtc);
    }

    private static string FormatGpsFix(GpsFix fix) => fix switch
    {
        GpsFix.None => "—",
        GpsFix.NoFix => "No Fix",
        GpsFix.Fix2D => "2D Fix",
        GpsFix.Fix3D => "3D Fix",
        GpsFix.Dgps => "DGPS",
        GpsFix.Rtk => "RTK",
        _ => "—"
    };

    private void UpdateGpsPresentation()
    {
        if (_flightState is null) return;

        GpsHealth = GpsHealthEvaluator.Evaluate(
            _flightState.IsDataValid,
            _flightState.GpsFix,
            _flightState.SatelliteCount,
            _flightState.GpsHdop);
        GpsHealthDisplay = GpsHealth switch
        {
            GpsHealthLevel.Healthy => "SAĞLIKLI",
            GpsHealthLevel.Warning => "UYARI",
            GpsHealthLevel.Critical => "ZAYIF",
            _ => "VERİ YOK"
        };
        var hdop = _flightState.GpsHdop is > 0
            ? _flightState.GpsHdop.Value.ToString("F2")
            : "—";
        GpsFixDisplay = $"{FormatGpsFix(_flightState.GpsFix).ToUpperInvariant()} · {_flightState.SatelliteCount} SAT · HDOP {hdop}";
    }
}
