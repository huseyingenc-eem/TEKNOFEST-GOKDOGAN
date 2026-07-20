using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Connection;
using GOKDOGANIHA.Core.Services.Alerts;
using GOKDOGANIHA.Core.Services.Alerts.Monitors;
using GOKDOGANIHA.Core.Services.Autonomy;
using GOKDOGANIHA.Core.Services.Failsafe;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Services.Safety;
using GOKDOGANIHA.Core.Services.Session;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.Core.Services.Time;

namespace GOKDOGANIHA.UI.Services;

/// <summary>
/// Immutable runtime dependency snapshot for the main UI. Static <see cref="App"/>
/// access is deliberately confined to this composition adapter; feature view
/// models receive only the services they actually use.
/// </summary>
internal sealed record MainWindowRuntimeServices
{
    public required ApplicationOptions Options { get; init; }
    public FlightState? FlightState { get; init; }
    public required IClock Clock { get; init; }
    public required AlertBus Alerts { get; init; }
    public required IDialogService Dialogs { get; init; }
    public required ConnectionStatus ServerConnection { get; init; }
    public required ConnectionStatus TelemetryConnection { get; init; }
    public required ConnectionStatus VideoConnection { get; init; }
    public FlightBackendCoordinator? FlightBackend { get; init; }
    public ConnectionOrchestrator? Connection { get; init; }
    public ServerClock? ServerClock { get; init; }
    public KilitlenmeDenetim? LockEngine { get; init; }
    public KamikazeFsm? Kamikaze { get; init; }
    public TelemetryHzMeter? HzMeter { get; init; }
    public BoundaryProximityMonitor? BoundaryProximity { get; init; }
    public HssProximityMonitor? HssProximity { get; init; }
    public CommLatencyMonitor? CommLatency { get; init; }
    public FailsafeMonitor? Failsafe { get; init; }
    public ManualTransitionCounter? ManualTransitions { get; init; }
    public IFlightCommandSink? Commands { get; init; }

    public static MainWindowRuntimeServices FromApp() => new()
    {
        Options = App.AppOptions,
        FlightState = App.FlightState,
        Clock = App.Clock,
        Alerts = App.AlertBus,
        Dialogs = App.Dialogs,
        ServerConnection = App.ServerConnection,
        TelemetryConnection = App.TelemetryConnection,
        VideoConnection = App.VideoConnection,
        FlightBackend = App.FlightBackend,
        Connection = App.Connection,
        ServerClock = App.ServerClock,
        LockEngine = App.LockEngine,
        Kamikaze = App.Kamikaze,
        HzMeter = App.HzMeter,
        BoundaryProximity = App.BoundaryProximity,
        HssProximity = App.HssProximity,
        CommLatency = App.CommLatency,
        Failsafe = App.Failsafe,
        ManualTransitions = App.ManualTransitions,
        Commands = App.Commands
    };
}
