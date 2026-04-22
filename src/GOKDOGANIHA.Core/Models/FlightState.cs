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
    private double _altitude;           // metre
    private double _pitch;              // derece
    private double _heading;            // derece (0-360)
    private double _roll;               // derece
    private double _speed;              // m/s
    private int _batteryPercent = 100;  // 0-100
    private int _batteryVoltage;        // UYARILAR eşiği Volt cinsinden; basitlik için int
    private bool _isAutonomous;
    private bool _isLocked;
    private int _targetCenterX, _targetCenterY, _targetWidth, _targetHeight;

    public double Latitude      { get => _latitude;       set => Set(ref _latitude, value); }
    public double Longitude     { get => _longitude;      set => Set(ref _longitude, value); }
    public double Altitude      { get => _altitude;       set => Set(ref _altitude, value); }
    public double Pitch         { get => _pitch;          set => Set(ref _pitch, value); }
    public double Heading       { get => _heading;        set => Set(ref _heading, value); }
    public double Roll          { get => _roll;           set => Set(ref _roll, value); }
    public double Speed         { get => _speed;          set => Set(ref _speed, value); }
    public int    BatteryPercent{ get => _batteryPercent; set => Set(ref _batteryPercent, value); }
    public int    BatteryVoltage{ get => _batteryVoltage; set => Set(ref _batteryVoltage, value); }
    public bool   IsAutonomous  { get => _isAutonomous;   set => Set(ref _isAutonomous, value); }
    public bool   IsLocked      { get => _isLocked;       set => Set(ref _isLocked, value); }
    public int    TargetCenterX { get => _targetCenterX;  set => Set(ref _targetCenterX, value); }
    public int    TargetCenterY { get => _targetCenterY;  set => Set(ref _targetCenterY, value); }
    public int    TargetWidth   { get => _targetWidth;    set => Set(ref _targetWidth, value); }
    public int    TargetHeight  { get => _targetHeight;   set => Set(ref _targetHeight, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
