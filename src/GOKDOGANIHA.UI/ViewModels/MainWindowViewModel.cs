using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Connection;
using GOKDOGANIHA.Core.Services.Alerts;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Services.Time;
using GOKDOGANIHA.UI.Services;
using GOKDOGANIHA.UI.ViewModels.Flight;
using GOKDOGANIHA.UI.ViewModels.Missions;
using GOKDOGANIHA.UI.ViewModels.Shell;
using GOKDOGANIHA.UI.ViewModels.Status;
using GOKDOGANIHA.UI.ViewModels.Video;

namespace GOKDOGANIHA.UI.ViewModels;

/// <summary>
/// Main-window composition model. Feature state and behaviour live in focused
/// child view models; this class only wires them together and owns their lifetime.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly DashboardRefreshLoop? _refreshLoop;

    public MainWindowViewModel()
        : this(
            new MapViewModel(),
            CreateRuntimeSettings(),
            System.Windows.Application.Current is App ? MainWindowRuntimeServices.FromApp() : null) { }

    public MainWindowViewModel(MapViewModel map)
        : this(map, new SettingsViewModel(), null) { }

    public MainWindowViewModel(MapViewModel map, SettingsViewModel settings)
        : this(map, settings, null) { }

    public MainWindowViewModel(TelemetryPollService telemetryPoll, HssPollService hssPoll)
        : this(new MapViewModel(telemetryPoll, hssPoll), CreateRuntimeSettings(),
            System.Windows.Application.Current is App ? MainWindowRuntimeServices.FromApp() : null) { }

    private MainWindowViewModel(
        MapViewModel map,
        SettingsViewModel settings,
        MainWindowRuntimeServices? runtime)
    {
        Map = map;
        Settings = settings;
        Overlay = new OverlayViewModel();

        var services = runtime ?? CreateDesignServices();
        Flight = new FlightTelemetryViewModel(
            map,
            services.FlightState,
            services.FlightBackend,
            services.LockEngine,
            services.Options.Mavlink,
            services.Options.Autonomy);
        SystemStatus = new SystemStatusViewModel(
            services.Clock,
            services.Connection,
            services.ServerClock,
            services.FlightBackend,
            services.HzMeter,
            services.BoundaryProximity,
            services.HssProximity,
            services.CommLatency,
            services.Failsafe,
            services.ManualTransitions,
            services.ServerConnection,
            services.TelemetryConnection,
            services.VideoConnection);
        Commands = new FlightCommandsViewModel(
            services.Commands,
            services.Dialogs,
            services.Alerts,
            services.Clock,
            Flight,
            map);
        Kamikaze = new KamikazeMissionViewModel(
            services.Kamikaze,
            services.FlightState,
            services.FlightBackend,
            services.Connection,
            services.Dialogs,
            services.Alerts,
            services.Clock,
            map,
            Overlay);
        Camera = new CameraViewModel(Flight);

        if (runtime is null) return;

        _refreshLoop = new DashboardRefreshLoop(
            Flight.Refresh,
            SystemStatus.Refresh,
            Kamikaze.Refresh);
        _refreshLoop.Start();
    }

    public MapViewModel Map { get; }
    public SettingsViewModel Settings { get; }
    public FlightTelemetryViewModel Flight { get; }
    public FlightCommandsViewModel Commands { get; }
    public SystemStatusViewModel SystemStatus { get; }
    public KamikazeMissionViewModel Kamikaze { get; }
    public OverlayViewModel Overlay { get; }
    public CameraViewModel Camera { get; }

    public void Dispose()
    {
        _refreshLoop?.Dispose();
        Kamikaze.Dispose();
        Flight.Dispose();
        Map.Dispose();
    }

    private static SettingsViewModel CreateRuntimeSettings()
        => App.SettingsFactory?.Create() ?? new SettingsViewModel();

    private static MainWindowRuntimeServices CreateDesignServices() => new()
    {
        Options = new ApplicationOptions(),
        Clock = new SystemClock(),
        Alerts = new AlertBus(),
        Dialogs = new DialogService(),
        ServerConnection = new ConnectionStatus("SUNUCU"),
        TelemetryConnection = new ConnectionStatus("TELEMETRİ"),
        VideoConnection = new ConnectionStatus("VİDEO")
    };
}
