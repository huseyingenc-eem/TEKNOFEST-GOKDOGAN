using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GOKDOGANIHA.Core.Models;

/// <summary>
/// İHA'nın anlık durumunu tek observable aggregate root olarak tutar.
/// Telemetri source (simulator / MAVLink adapter / manuel) buraya yazar;
/// tüketiciler (TelemetryPacketBuilder, Monitors, UI binding) buradan okur.
/// CommunityToolkit.Mvvm kullanmadan elle INPC — Core WPF'e bağımlı değil.
/// </summary>
public sealed class FlightState : INotifyPropertyChanged
{
    private double _latitude;
    private double _longitude;
    private double _altitude;           // metre (AGL)
    private double _pitch;              // derece
    private double _heading;            // derece (0-360)
    private double _roll;               // derece
    private double _groundSpeed;        // m/s — yer hızı
    private double _airspeed;           // m/s — hava hızı (pitot)
    private double _verticalSpeed;      // m/s — climb rate
    private double _wpDistance;         // m — bir sonraki WP'ye mesafe
    private int _batteryPercent = 100;  // 0-100
    private int _batteryVoltage;        // UYARILAR eşiği Volt cinsinden; basitlik için int
    private bool _isAutonomous;
    private bool _isLocked;
    private bool _isArmed;
    private FlightMode _mode = FlightMode.Manual;
    private GpsFix _gpsFix = GpsFix.None;
    private int _satelliteCount;
    private int? _targetTeamNumber;     // şu an kilitlenmeye çalışılan rakip ID
    private double _signalRssi;         // telemetri link gücü (dBm; negatif normaldir)
    private int _targetCenterX, _targetCenterY, _targetWidth, _targetHeight;
    private bool _isDataValid;
    private string _dataSource = "NONE";
    private DateTime? _lastUpdatedUtc;
    private DateTime? _gpsTimeUtc;
    private long _sequence;

    public double Latitude       { get => _latitude;        set => Set(ref _latitude, value); }
    public double Longitude      { get => _longitude;       set => Set(ref _longitude, value); }
    public double Altitude       { get => _altitude;        set => Set(ref _altitude, value); }
    public double Pitch          { get => _pitch;           set => Set(ref _pitch, value); }
    public double Heading        { get => _heading;         set => Set(ref _heading, value); }
    public double Roll           { get => _roll;            set => Set(ref _roll, value); }
    public double GroundSpeed    { get => _groundSpeed;     set => Set(ref _groundSpeed, value); }
    public double Airspeed       { get => _airspeed;        set => Set(ref _airspeed, value); }
    public double VerticalSpeed  { get => _verticalSpeed;   set => Set(ref _verticalSpeed, value); }
    public double WpDistance     { get => _wpDistance;      set => Set(ref _wpDistance, value); }
    public int    BatteryPercent { get => _batteryPercent;  set => Set(ref _batteryPercent, value); }
    public int    BatteryVoltage { get => _batteryVoltage;  set => Set(ref _batteryVoltage, value); }
    public bool   IsAutonomous   { get => _isAutonomous;    set => Set(ref _isAutonomous, value); }
    public bool   IsLocked       { get => _isLocked;        set => Set(ref _isLocked, value); }
    public bool   IsArmed        { get => _isArmed;         set => Set(ref _isArmed, value); }
    public FlightMode Mode       { get => _mode;            set => Set(ref _mode, value); }
    public GpsFix GpsFix         { get => _gpsFix;          set => Set(ref _gpsFix, value); }
    public int    SatelliteCount { get => _satelliteCount;  set => Set(ref _satelliteCount, value); }
    public int?   TargetTeamNumber { get => _targetTeamNumber; set => Set(ref _targetTeamNumber, value); }
    public double SignalRssi     { get => _signalRssi;      set => Set(ref _signalRssi, value); }
    public int    TargetCenterX  { get => _targetCenterX;   set => Set(ref _targetCenterX, value); }
    public int    TargetCenterY  { get => _targetCenterY;   set => Set(ref _targetCenterY, value); }
    public int    TargetWidth    { get => _targetWidth;     set => Set(ref _targetWidth, value); }
    public int    TargetHeight   { get => _targetHeight;    set => Set(ref _targetHeight, value); }
    public bool   IsDataValid    { get => _isDataValid;     private set => Set(ref _isDataValid, value); }
    public string DataSource     { get => _dataSource;      private set => Set(ref _dataSource, value); }
    public DateTime? LastUpdatedUtc { get => _lastUpdatedUtc; private set => Set(ref _lastUpdatedUtc, value); }
    public DateTime? GpsTimeUtc  { get => _gpsTimeUtc;      private set => Set(ref _gpsTimeUtc, value); }
    public long Sequence         { get => _sequence;        private set => Set(ref _sequence, value); }

    public void Touch(string source, DateTime? gpsTimeUtc = null)
    {
        DataSource = source;
        GpsTimeUtc = gpsTimeUtc;
        LastUpdatedUtc = DateTime.UtcNow;
        Sequence++;
        IsDataValid = true;
    }

    public void MarkUnavailable(string source)
    {
        DataSource = source;
        IsDataValid = false;
        LastUpdatedUtc = null;
        GpsTimeUtc = null;
    }

    public bool IsFresh(DateTime nowUtc, TimeSpan maxAge)
        => IsDataValid && LastUpdatedUtc is { } last && nowUtc - last <= maxAge;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
