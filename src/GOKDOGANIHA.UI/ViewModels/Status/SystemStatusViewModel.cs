using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Connection;
using GOKDOGANIHA.Core.Services.Alerts.Monitors;
using GOKDOGANIHA.Core.Services.Failsafe;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Services.Safety;
using GOKDOGANIHA.Core.Services.Session;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.Core.Services.Time;

namespace GOKDOGANIHA.UI.ViewModels.Status;

public partial class SystemStatusViewModel : ObservableObject
{
    private readonly IClock _clock;
    private readonly ConnectionOrchestrator? _connection;
    private readonly ServerClock? _serverClock;
    private readonly FlightBackendCoordinator? _flightBackend;
    private readonly TelemetryHzMeter? _hzMeter;
    private readonly BoundaryProximityMonitor? _boundaryProximity;
    private readonly HssProximityMonitor? _hssProximity;
    private readonly CommLatencyMonitor? _commLatency;
    private readonly FailsafeMonitor? _failsafe;
    private readonly ManualTransitionCounter? _manualTransitions;

    internal SystemStatusViewModel(
        IClock clock,
        ConnectionOrchestrator? connection,
        ServerClock? serverClock,
        FlightBackendCoordinator? flightBackend,
        TelemetryHzMeter? hzMeter,
        BoundaryProximityMonitor? boundaryProximity,
        HssProximityMonitor? hssProximity,
        CommLatencyMonitor? commLatency,
        FailsafeMonitor? failsafe,
        ManualTransitionCounter? manualTransitions,
        ConnectionStatus serverConnection,
        ConnectionStatus telemetryConnection,
        ConnectionStatus videoConnection)
    {
        _clock = clock;
        _connection = connection;
        _serverClock = serverClock;
        _flightBackend = flightBackend;
        _hzMeter = hzMeter;
        _boundaryProximity = boundaryProximity;
        _hssProximity = hssProximity;
        _commLatency = commLatency;
        _failsafe = failsafe;
        _manualTransitions = manualTransitions;
        ServerConnection = serverConnection;
        TelemetryConnection = telemetryConnection;
        VideoConnection = videoConnection;
    }

    public ConnectionStatus ServerConnection { get; }
    public ConnectionStatus TelemetryConnection { get; }
    public ConnectionStatus VideoConnection { get; }

    [ObservableProperty] private string _serverTime = "00:00:00.000";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private double _telemetryHzMeasured;
    [ObservableProperty] private string _telemetryStatus = "—";
    [ObservableProperty] private double _serverLatencyLiveMs;
    [ObservableProperty] private bool _hasTelemetrySignal;
    [ObservableProperty] private bool _isInsideBoundary = true;
    [ObservableProperty] private double _distanceToBoundaryEdge;
    [ObservableProperty] private int _hssViolationCount;
    [ObservableProperty] private double _hssViolationSeconds;
    [ObservableProperty] private int _manualTransitionCount;
    [ObservableProperty] private bool _isFailsafeActive;
    [ObservableProperty] private int _commDropoutCount;

    public void Refresh(DateTime nowUtc)
    {
        IsConnected = _connection?.IsConnected == true;
        var now = _serverClock?.Now ?? _clock.UtcNow;
        ServerTime = now.ToString("dd-HH:mm:ss.fff");

        if (_hzMeter is not null)
        {
            _hzMeter.Refresh();
            TelemetryHzMeasured = _hzMeter.CurrentHz;
            TelemetryStatus = _hzMeter.LastStatus;
            ServerLatencyLiveMs = _hzMeter.LastLatencyMs;
            HasTelemetrySignal = _hzMeter.LastReceivedUtc.HasValue;
        }

        if (_boundaryProximity is not null)
        {
            DistanceToBoundaryEdge = _boundaryProximity.DistanceToEdgeMeters;
            IsInsideBoundary = _boundaryProximity.IsInside;
        }

        if (_hssProximity is not null)
        {
            HssViolationCount = _hssProximity.ActiveViolationCount;
            HssViolationSeconds = _hssProximity.ActiveViolationDuration.TotalSeconds;
        }

        if (_commLatency is not null)
            CommDropoutCount = _commLatency.DropoutCount;
        if (_failsafe is not null)
            IsFailsafeActive = _failsafe.IsGcsLost;
        if (_manualTransitions is not null)
            ManualTransitionCount = _manualTransitions.Count;
    }

    [RelayCommand]
    private async Task RetryServerConnection()
    {
        if (_connection is not null)
            await _connection.ConnectAsync();
    }

    [RelayCommand]
    private async Task RetryTelemetry()
    {
        if (_flightBackend is not null)
            await _flightBackend.SwitchAsync(FlightDataMode.Live, forceRestart: true);
    }
}
