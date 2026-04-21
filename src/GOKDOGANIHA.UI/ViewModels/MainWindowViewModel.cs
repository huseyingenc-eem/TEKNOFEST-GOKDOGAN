using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel() : this(new MapViewModel()) { }

    public MainWindowViewModel(MapViewModel mapVm)
    {
        MapVm = mapVm;
    }

    public MainWindowViewModel(TelemetryPollService telemetryPoll, HssPollService hssPoll)
        : this(new MapViewModel(telemetryPoll, hssPoll)) { }

    public MapViewModel MapVm { get; }

    [ObservableProperty] private string _callSign = "GÖKDOĞAN-1";
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
}
