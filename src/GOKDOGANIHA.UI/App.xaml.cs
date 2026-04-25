using System;
using System.Threading.Tasks;
using System.Windows;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Alerts;
using GOKDOGANIHA.Core.Services.Alerts.Monitors;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Autonomy;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Services.Session;
using GOKDOGANIHA.Core.Services.Failsafe;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.Core.Services.Time;
using GOKDOGANIHA.UI.Services;

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
    public static ServerClock? ServerClock { get; private set; }
    public static BatteryMonitor? Battery { get; private set; }
    public static BoundaryProximityMonitor? BoundaryProximity { get; private set; }
    public static OpponentProximityMonitor? OpponentProximity { get; private set; }
    public static HssProximityMonitor? HssProximity { get; private set; }
    public static CommLatencyMonitor? CommLatency { get; private set; }
    public static FailsafeMonitor? Failsafe { get; private set; }
    public static KilitlenmeDenetim? LockEngine { get; private set; }
    public static IFlightCommandSink? Commands { get; private set; }
    public static ConnectionOrchestrator? Connection { get; private set; }
    public static IDialogService Dialogs { get; } = new DialogService();
    public static ISettingsViewModelFactory? SettingsFactory { get; private set; }

    private PeriodicTimer? _packetPump;
    private CancellationTokenSource? _pumpCts;
    private Task? _pumpTask;
    private PeriodicTimer? _failsafePump;
    private Task? _failsafeTask;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Catch anything that would otherwise silently kill the app so we
        // can see the stack instead of a closing window. A MessageBox is
        // intrusive but acceptable during development — competition build
        // should route this to a log file.
        DispatcherUnhandledException += (_, args) =>
        {
            // UI thread'inde modal dialog güvenli — process zaten interaktif.
            MessageBox.Show(
                $"Unhandled UI exception:\n\n{args.Exception}",
                "GÖKDOĞAN — Crash",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            // Background thread'den MessageBox.Show açmak shutdown sırasında hang'a sebep
            // olabilir (görünmez modal dialog process'i tutar). Sadece Debug'a yaz.
            System.Diagnostics.Debug.WriteLine($"[FATAL] Unhandled domain exception: {args.ExceptionObject}");
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
            BoundaryProximity = new BoundaryProximityMonitor(FlightState, AppOptions.Alerts, AlertBus, Clock);
            OpponentProximity = new OpponentProximityMonitor(FlightState, AppOptions.Alerts, AlertBus, Clock, TelemetryPoll);
            HssProximity = new HssProximityMonitor(FlightState, AppOptions.Alerts, AlertBus, Clock, HssPoll);
            CommLatency = new CommLatencyMonitor(AppOptions.Alerts, AlertBus, Clock, TelemetryPoll);
            Commands = new NullFlightCommandSink();
            Failsafe = new FailsafeMonitor(AppOptions.Failsafe, AlertBus, Commands, Clock);
            Connection = new ConnectionOrchestrator(client, TelemetryPoll, HssPoll, AlertBus, Clock);
            ServerClock = new ServerClock(client, Clock);
            ServerClock.Start();
            LockEngine = new KilitlenmeDenetim(AppOptions.Autonomy, Clock);
            LockEngine.LockSucceeded += OnLockSucceeded;
            SettingsFactory = new SettingsViewModelFactory(AppOptions, client, Dialogs, Connection);

            // Telemetri her başarılı cevabında failsafe heartbeat'i resetle
            TelemetryPoll.TelemetryReceived += (_, _) => Failsafe?.RecordHeartbeat();

            // Simülatör SADECE Settings'ten açıkça istenirse çalışır.
            // Varsayılan: kapalı → telemetri sıfır kalır, gerçek MAVLink adapter
            // beslemediği sürece UI'da uydurma veri yoktur.
            if (AppOptions.Telemetry.UseSimulator) FlightSimulator.Start();

            // Packet pump — FlightState'ten paket kurup TelemetryPoll'a besler
            StartPacketPump();

            // Failsafe tick — 1 Hz, GCS timeout'u değerlendirir
            StartFailsafePump();

            // Hz + UseSimulator ayar değişikliklerini canlı uygula.
            AppOptions.Telemetry.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppOptions.Telemetry.Hz))
                {
                    var hz = Math.Max(0.1, AppOptions.Telemetry.Hz);
                    TelemetryPoll?.SetInterval(TimeSpan.FromSeconds(1.0 / hz));
                }
                else if (e.PropertyName == nameof(AppOptions.Telemetry.UseSimulator))
                {
                    if (AppOptions.Telemetry.UseSimulator) FlightSimulator?.Start();
                    else _ = FlightSimulator?.StopAsync();
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Service bootstrap failed: {ex.Message}");
        }
    }

    private static async void OnLockSucceeded(object? sender, LockSuccessEventArgs e)
    {
        if (GameServer is null) return;
        try
        {
            var now = ServerClock?.IsSynchronized == true ? ServerClock.Now : Clock.UtcNow;
            var bitis = new ServerTime(now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
            await GameServer.KilitlenmeBilgisiGonderAsync(
                new KilitlenmeBilgisi(bitis, OtonomKilitlenme: FlightState.IsAutonomous ? 1 : 0));
            AlertBus.Publish(Alert.Create(
                kind: "lock.success",
                level: AlertLevel.Info,
                title: $"KİLİTLENME · T-{e.TargetId}",
                message: $"Kilitlenme paketi gönderildi (otonom: {(FlightState.IsAutonomous ? "evet" : "hayır")})",
                timeUtc: e.TimestampUtc));
        }
        catch (Exception ex)
        {
            AlertBus.Publish(Alert.Create(
                kind: "lock.error",
                level: AlertLevel.Warn,
                title: "KİLİTLENME paket hatası",
                message: ex.Message,
                timeUtc: Clock.UtcNow));
        }
    }

    private void StartFailsafePump()
    {
        _failsafePump = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _failsafeTask = Task.Run(async () =>
        {
            try
            {
                while (await _failsafePump!.WaitForNextTickAsync(_pumpCts!.Token))
                    Failsafe?.Tick();
            }
            catch (OperationCanceledException) { }
        }, _pumpCts!.Token);
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
                    // gps_saati: ServerClock sync olduysa onu, değilse lokal sistem saatini kullan.
                    var now = ServerClock?.IsSynchronized == true ? ServerClock.Now : Clock.UtcNow;
                    var gps = new ServerTime(now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
                    TelemetryPoll.UpdateOwnTelemetry(PacketBuilder.Build(gps));
                }
            }
            catch (OperationCanceledException) { }
        }, _pumpCts.Token);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Tüm dispose çağrıları izolasyonlu — biri hang ederse diğerlerini bloklamasın.
        // Background async loop'lar için her servisin Dispose'u zaten timeout'lu (500ms).
        SafeDispose(() => _pumpCts?.Cancel(), "pumpCts.Cancel");
        SafeDispose(() => _packetPump?.Dispose(), "packetPump");
        SafeDispose(() => _failsafePump?.Dispose(), "failsafePump");
        SafeDispose(() => ServerClock?.Dispose(), "ServerClock");
        SafeDispose(() => FlightSimulator?.Dispose(), "FlightSimulator");
        SafeDispose(() => Battery?.Dispose(), "Battery");
        SafeDispose(() => BoundaryProximity?.Dispose(), "BoundaryProximity");
        SafeDispose(() => OpponentProximity?.Dispose(), "OpponentProximity");
        SafeDispose(() => HssProximity?.Dispose(), "HssProximity");
        SafeDispose(() => CommLatency?.Dispose(), "CommLatency");
        SafeDispose(() => TelemetryPoll?.Dispose(), "TelemetryPoll");
        SafeDispose(() => HssPoll?.Dispose(), "HssPoll");
        SafeDispose(() => (GameServer as IDisposable)?.Dispose(), "GameServer");

        base.OnExit(e);

        // Son çare — bazı thread'ler (HttpClient socket pool, GMap.NET tile loader vs.)
        // background olmasına rağmen process'i tutabiliyor. 1.5 sn daha bekleyip zorla çık.
        // Bu olmadan WPF "X" basıldığında pencere kapanır ama .exe arka planda kalır → derleyici dll'i kilitleyemez.
        Task.Delay(1500).ContinueWith(_ => Environment.Exit(0), TaskScheduler.Default);
    }

    private static void SafeDispose(Action action, string name)
    {
        try { action(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OnExit] {name} dispose failed: {ex.Message}"); }
    }

    // FailsafeMonitor komut sink'ine ihtiyaç duyar; gerçek MAVLink adapter
    // gelene kadar log-only stub. Komutları AlertBus'a Info olarak yayar — UI'da
    // CommandsPanel butonları için görsel feedback bu sayede çalışır.
    private sealed class NullFlightCommandSink : IFlightCommandSink
    {
        public void Arm()                                                 => Emit("ARM",     "Motor armed (stub)");
        public void Disarm()                                              => Emit("DISARM",  "Motor disarmed (stub)");
        public void SetMode(FlightMode mode)                              => Emit("MODE",    $"Mod değiştirildi → {mode} (stub)");
        public void Rtl()                                                 => Emit("RTL",     "Eve dön (stub)");
        public void Land()                                                => Emit("LAND",    "İniş komutu (stub)");
        public void Loiter()                                              => Emit("LOITER",  "Loiter komutu (stub)");
        public void GotoWaypoint(double lat, double lon, double altMeters)
            => Emit("GOTO", $"Waypoint git ({lat:F5}, {lon:F5}, {altMeters:F0}m) (stub)");

        private static void Emit(string kind, string message)
        {
            System.Diagnostics.Debug.WriteLine($"[FC] {kind}: {message}");
            AlertBus.Publish(Alert.Create(
                kind: $"command.{kind.ToLowerInvariant()}",
                level: AlertLevel.Info,
                title: $"FC · {kind}",
                message: message,
                timeUtc: Clock.UtcNow));
        }
    }
}
