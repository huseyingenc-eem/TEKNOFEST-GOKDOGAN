using System;
using System.Threading.Tasks;
using System.Windows;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Alerts;
using GOKDOGANIHA.Core.Services.Alerts.Monitors;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Services.Session;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.Core.Services.Time;

namespace GOKDOGANIHA.UI;

public partial class App : Application
{
    // ===== Composition root =====
    public static ApplicationOptions AppOptions { get; } = new();
    public static GameServerOptions ServerOptions => AppOptions.GameServer;
    public static FlightState FlightState { get; } = new();
    public static IClock Clock { get; } = new SystemClock();
    public static AlertBus AlertBus { get; } = new();

    // Services built in OnStartup
    public static IGameServerClient? GameServer { get; private set; }
    public static TelemetryPollService? TelemetryPoll { get; private set; }
    public static HssPollService? HssPoll { get; private set; }
    public static TelemetryPacketBuilder? PacketBuilder { get; private set; }
    public static SimulatedFlightSource? FlightSimulator { get; private set; }
    public static BatteryMonitor? Battery { get; private set; }
    public static ConnectionOrchestrator? Connection { get; private set; }

    private PeriodicTimer? _packetPump;
    private CancellationTokenSource? _pumpCts;
    private Task? _pumpTask;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Catch anything that would otherwise silently kill the app so we
        // can see the stack instead of a closing window. A MessageBox is
        // intrusive but acceptable during development — competition build
        // should route this to a log file.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Unhandled UI exception:\n\n{args.Exception}",
                "GÖKDOĞAN — Crash",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Unhandled domain exception:\n\n{args.ExceptionObject}",
                "GÖKDOĞAN — Crash",
                MessageBoxButton.OK, MessageBoxImage.Error);
        };

        base.OnStartup(e);
        try
        {
            var client = new GameServerClient(AppOptions.GameServer);
            GameServer = client;
            TelemetryPoll = new TelemetryPollService(
                client,
                TimeSpan.FromSeconds(1.0 / Math.Max(0.1, AppOptions.Telemetry.Hz)));
            HssPoll = new HssPollService(client);

            PacketBuilder = new TelemetryPacketBuilder(FlightState, AppOptions.GameServer);
            FlightSimulator = new SimulatedFlightSource(FlightState);
            Battery = new BatteryMonitor(FlightState, AppOptions.Alerts, AlertBus, Clock);
            Connection = new ConnectionOrchestrator(client, TelemetryPoll, HssPoll, AlertBus, Clock);

            // FlightState'i simülasyonla besle (gerçek hardware gelene kadar)
            FlightSimulator.Start();

            // Packet pump — FlightState'ten paket kurup TelemetryPoll'a besler
            StartPacketPump();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Service bootstrap failed: {ex.Message}");
        }
    }

    private void StartPacketPump()
    {
        _pumpCts = new CancellationTokenSource();
        _packetPump = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        _pumpTask = Task.Run(async () =>
        {
            try
            {
                while (await _packetPump!.WaitForNextTickAsync(_pumpCts!.Token))
                {
                    if (PacketBuilder is null || TelemetryPoll is null) continue;
                    // gps_saati için basit bir fallback — gerçek sistem saatinden türet.
                    var now = Clock.UtcNow;
                    var gps = new ServerTime(now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
                    TelemetryPoll.UpdateOwnTelemetry(PacketBuilder.Build(gps));
                }
            }
            catch (OperationCanceledException) { }
        }, _pumpCts.Token);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _pumpCts?.Cancel();
        _packetPump?.Dispose();
        FlightSimulator?.Dispose();
        Battery?.Dispose();
        TelemetryPoll?.Dispose();
        HssPoll?.Dispose();
        (GameServer as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}
