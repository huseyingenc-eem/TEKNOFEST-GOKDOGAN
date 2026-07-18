using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Connection;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Alerts;
using GOKDOGANIHA.Core.Services.Alerts.Monitors;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Autonomy;
using GOKDOGANIHA.Core.Services.Persistence;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Services.Safety;
using GOKDOGANIHA.Core.Services.Session;
using GOKDOGANIHA.Core.Services.Failsafe;
using GOKDOGANIHA.Core.Services.Mavlink;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.Core.Services.Time;
using GOKDOGANIHA.UI.Services;

namespace GOKDOGANIHA.UI;

public partial class App : Application
{
    // ===== Composition root =====
    // SettingsStore boot'ta diskten yükler — ApplicationOptions snapshot ile başlatılır.
    private static readonly SettingsStore _settingsStore = new();
    public static ApplicationOptions AppOptions { get; } = _settingsStore.Load();
    public static GameServerOptions ServerOptions => AppOptions.GameServer;
    public static FlightState FlightState { get; } = new();
    public static IClock Clock { get; } = new SystemClock();
    public static AlertBus AlertBus { get; } = new();

    // Bağlantı durumu göstergeleri — servisler ilerletir, UI (TopBar) gözlemler.
    // Tek ortak dil (ConnectionStatus) sayesinde her bağlantı aynı rozetle gösterilir.
    public static ConnectionStatus ServerConnection { get; } = new("SUNUCU");
    public static ConnectionStatus TelemetryConnection { get; } = new("TELEMETRİ");
    public static ConnectionStatus VideoConnection { get; } = new("VİDEO");

    // Services built in OnStartup
    public static IGameServerClient? GameServer { get; private set; }
    public static TelemetryPollService? TelemetryPoll { get; private set; }
    public static HssPollService? HssPoll { get; private set; }
    public static TelemetryPacketBuilder? PacketBuilder { get; private set; }
    public static SimulatedFlightSource? FlightSimulator { get; private set; }
    public static MavlinkFlightStateSource? MavlinkSource { get; private set; }
    public static FlightBackendCoordinator? FlightBackend { get; private set; }
    public static ServerClock? ServerClock { get; private set; }
    public static BatteryMonitor? Battery { get; private set; }
    public static BoundaryProximityMonitor? BoundaryProximity { get; private set; }
    public static OpponentProximityMonitor? OpponentProximity { get; private set; }
    public static HssProximityMonitor? HssProximity { get; private set; }
    public static CommLatencyMonitor? CommLatency { get; private set; }
    public static FailsafeMonitor? Failsafe { get; private set; }
    public static KilitlenmeDenetim? LockEngine { get; private set; }
    public static KamikazeFsm? Kamikaze { get; private set; }
    public static TelemetryHzMeter? HzMeter { get; private set; }
    public static ManualTransitionCounter? ManualTransitions { get; private set; }
    public static IFlightCommandSink? Commands { get; private set; }
    public static ConnectionOrchestrator? Connection { get; private set; }
    public static IDialogService Dialogs { get; } = new DialogService();
    public static ISettingsViewModelFactory? SettingsFactory { get; private set; }

    private PeriodicTimer? _packetPump;
    private CancellationTokenSource? _pumpCts;
    private Task? _pumpTask;
    private PeriodicTimer? _failsafePump;
    private Task? _failsafeTask;
    private System.Windows.Threading.DispatcherTimer? _settingsDebounceTimer;

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
                TimeSpan.FromSeconds(1.0 / Math.Clamp(AppOptions.Telemetry.Hz, 1.0, 2.0)));
            HssPoll = new HssPollService(client);

            PacketBuilder = new TelemetryPacketBuilder(FlightState, AppOptions.GameServer);
            FlightSimulator = new SimulatedFlightSource(FlightState);
            MavlinkSource = new MavlinkFlightStateSource(FlightState, AppOptions.Mavlink);
            Battery = new BatteryMonitor(FlightState, AppOptions.Alerts, AlertBus, Clock);
            BoundaryProximity = new BoundaryProximityMonitor(FlightState, AppOptions.Alerts, AlertBus, Clock);
            OpponentProximity = new OpponentProximityMonitor(FlightState, AppOptions.Alerts, AlertBus, Clock, TelemetryPoll);
            HssProximity = new HssProximityMonitor(FlightState, AppOptions.Alerts, AlertBus, Clock, HssPoll);
            CommLatency = new CommLatencyMonitor(AppOptions.Alerts, AlertBus, Clock, TelemetryPoll);
            var transitionBlocked = new BlockedFlightCommandSink(
                "uçuş veri kaynağı geçiş halinde",
                message => PublishCommandFeedback(message, AlertLevel.Warn));
            var commandProxy = new SwitchableFlightCommandSink(transitionBlocked);
            var liveCommands = new BlockedFlightCommandSink(
                "canlı MAVLink komut adaptörü henüz etkin değil; yalnızca telemetri okunuyor",
                message => PublishCommandFeedback(message, AlertLevel.Warn));
            var simulationCommands = new SimulatedFlightCommandSink(
                FlightState,
                message => PublishCommandFeedback($"SIM · {message}", AlertLevel.Info));
            Commands = commandProxy;
            FlightBackend = new FlightBackendCoordinator(
                FlightState,
                MavlinkSource,
                FlightSimulator,
                liveCommands,
                simulationCommands,
                commandProxy,
                transitionBlocked);
            // Telemetri (uçuş veri kaynağı) durumunu ortak bağlantı göstergesine köprüle.
            FlightBackend.PropertyChanged += OnFlightBackendConnectionChanged;
            MapFlightBackendToConnection();
            Failsafe = new FailsafeMonitor(AppOptions.Failsafe, AlertBus, Commands, Clock);
            Connection = new ConnectionOrchestrator(client, TelemetryPoll, HssPoll, AlertBus, Clock, AppOptions.Telemetry, ServerConnection);
            TelemetryPoll.PacketRejected += (_, validation) =>
                AlertBus.Publish(Alert.Create(
                    kind: "telemetry.validation",
                    level: AlertLevel.Danger,
                    title: "TELEMETRİ PAKETİ ENGELLENDİ",
                    message: validation.Message,
                    timeUtc: Clock.UtcNow));
            ServerClock = new ServerClock(client, Clock);
            ServerClock.Start();
            LockEngine = new KilitlenmeDenetim(AppOptions.Autonomy, Clock);
            LockEngine.LockSucceeded += OnLockSucceeded;
            Kamikaze = new KamikazeFsm(AppOptions.Autonomy, Clock);
            Kamikaze.MissionCompleted += OnKamikazeCompleted;
            HzMeter = new TelemetryHzMeter(TelemetryPoll, Clock);
            ManualTransitions = new ManualTransitionCounter(FlightState);
            SettingsFactory = new SettingsViewModelFactory(AppOptions, client, Dialogs, Connection, FlightBackend);

            // Araç kaynağından her geçerli frame geldiğinde araç-link heartbeat'i yenilenir.
            FlightState.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(FlightState.LastUpdatedUtc)
                    && FlightState.IsDataValid)
                    Failsafe?.RecordHeartbeat();
            };

            // Aktif mod session state'tir: kayıtlı eski simülasyon tercihi okunmaz.
            // Uygulama her açılışta salt-okunur canlı MAVLink dinleyicisiyle başlar.
            _ = StartDefaultLiveBackendAsync();

            // Packet pump — FlightState'ten paket kurup TelemetryPoll'a besler
            StartPacketPump();

            // Failsafe tick — 1 Hz, GCS timeout'u değerlendirir
            StartFailsafePump();

            // Gönderim Hz ayar değişikliğini canlı uygula.
            AppOptions.Telemetry.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppOptions.Telemetry.Hz))
                {
                    var hz = Math.Clamp(AppOptions.Telemetry.Hz, 1.0, 2.0);
                    TelemetryPoll?.SetInterval(TimeSpan.FromSeconds(1.0 / hz));
                }
                ScheduleSettingsSave();
            };

            // Diğer INPC'li option grupları (Telemetry hariç — yukarıda ele alındı).
            AppOptions.Video.PropertyChanged += (_, _) => ScheduleSettingsSave();
            AppOptions.Map.PropertyChanged   += (_, _) => ScheduleSettingsSave();
            AppOptions.Mavlink.PropertyChanged += (_, _) => ScheduleSettingsSave();
            AppOptions.GameServer.PropertyChanged += (_, _) => ScheduleSettingsSave();
            // Alerts/Failsafe INPC değil; uygulama kapanışında kaydedilir.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Service bootstrap failed: {ex.Message}");
        }
    }

    private static async void OnKamikazeCompleted(object? sender, KamikazeMissionResult e)
    {
        if (!e.Success)
        {
            AlertBus.Publish(Alert.Create(
                kind: "kamikaze.failed",
                level: AlertLevel.Warn,
                title: "KAMİKAZE BAŞARISIZ",
                message: e.Reason,
                timeUtc: e.CompletedAtUtc));
            return;
        }
        if (GameServer is null || !CanTransmitCompetitionData())
        {
            PublishCompetitionGuardAlert("Kamikaze sonucu gönderilmedi");
            return;
        }
        try
        {
            var start = Kamikaze?.MissionStartUtc ?? Clock.UtcNow;
            var bitis = ServerClock?.IsSynchronized == true ? ServerClock.Now : e.CompletedAtUtc;
            var bilgi = new KamikazeBilgisi(
                BaslangicZamani: CompetitionTime.FromUtc(start),
                BitisZamani:    CompetitionTime.FromUtc(bitis),
                QrMetni:        e.QrText);
            await GameServer.KamikazeBilgisiGonderAsync(bilgi);
            AlertBus.Publish(Alert.Create(
                kind: "kamikaze.success",
                level: AlertLevel.Info,
                title: "KAMİKAZE TAMAM",
                message: $"QR: {e.QrText} · paket gönderildi",
                timeUtc: e.CompletedAtUtc));
        }
        catch (Exception ex)
        {
            AlertBus.Publish(Alert.Create(
                kind: "kamikaze.error",
                level: AlertLevel.Warn,
                title: "KAMİKAZE paket hatası",
                message: ex.Message,
                timeUtc: Clock.UtcNow));
        }
    }

    private static async void OnLockSucceeded(object? sender, LockSuccessEventArgs e)
    {
        if (GameServer is null || !CanTransmitCompetitionData())
        {
            PublishCompetitionGuardAlert("Kilitlenme sonucu gönderilmedi");
            return;
        }
        try
        {
            var now = ServerClock?.IsSynchronized == true ? ServerClock.Now : Clock.UtcNow;
            var bitis = CompetitionTime.FromUtc(now);
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

    private static void OnFlightBackendConnectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FlightBackendCoordinator.Status)
            or nameof(FlightBackendCoordinator.StatusMessage)
            or nameof(FlightBackendCoordinator.ActiveMode))
            MapFlightBackendToConnection();
    }

    /// <summary>
    /// FlightBackend durumunu ortak <see cref="ConnectionStatus"/> diline çevirir; böylece
    /// telemetri de sunucu ile ayni rozet/renk/retry davranışını paylaşır.
    /// </summary>
    private static void MapFlightBackendToConnection()
    {
        if (FlightBackend is null) return;
        var msg = string.IsNullOrWhiteSpace(FlightBackend.StatusMessage)
            ? "Veri bekleniyor"
            : FlightBackend.StatusMessage;
        switch (FlightBackend.Status)
        {
            case FlightBackendStatus.Live:
                TelemetryConnection.MarkOnline(msg);
                break;
            case FlightBackendStatus.Simulation:
                TelemetryConnection.MarkOnline($"Simülasyon · {msg}");
                break;
            case FlightBackendStatus.ConnectingLive:
            case FlightBackendStatus.StartingSimulation:
            case FlightBackendStatus.Switching:
                TelemetryConnection.MarkConnecting(msg);
                break;
            case FlightBackendStatus.Faulted:
                TelemetryConnection.MarkFaulted(msg);
                break;
            default:
                TelemetryConnection.MarkOffline(msg);
                break;
        }
    }

    private static async Task StartDefaultLiveBackendAsync()
    {
        if (FlightBackend is null) return;
        var result = await FlightBackend.SwitchAsync(FlightDataMode.Live);
        if (!result.Success)
        {
            AlertBus.Publish(Alert.Create(
                kind: "backend.start",
                level: AlertLevel.Danger,
                title: "CANLI VERİ KAYNAĞI BAŞLATILAMADI",
                message: result.Message,
                timeUtc: Clock.UtcNow));
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
                    var transmissionAllowed = CanTransmitCompetitionData();
                    TelemetryPoll.SetTransmissionEnabled(transmissionAllowed);
                    if (!transmissionAllowed) continue;

                    // Test/sim akışında fallback saat; resmî canlı akışta builder bunu
                    // doğrudan araçtan gelen GPS UTC zamanı ile değiştirir.
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
        // Pending settings değişikliği varsa hemen flush — kullanıcı çıkış öncesi
        // yaptığı değişiklik (örn. takım numarası) kaybolmasın.
        SafeDispose(() => _settingsStore.Save(AppOptions), "settingsStore.Save");

        // Tüm dispose çağrıları izolasyonlu — biri hang ederse diğerlerini bloklamasın.
        // Background async loop'lar için her servisin Dispose'u zaten timeout'lu (500ms).
        SafeDispose(() => _pumpCts?.Cancel(), "pumpCts.Cancel");
        SafeDispose(() => _packetPump?.Dispose(), "packetPump");
        SafeDispose(() => _failsafePump?.Dispose(), "failsafePump");
        SafeDispose(() => ServerClock?.Dispose(), "ServerClock");
        SafeDispose(() => HzMeter?.Dispose(), "HzMeter");
        SafeDispose(() => ManualTransitions?.Dispose(), "ManualTransitions");
        SafeDispose(() => FlightBackend?.Dispose(), "FlightBackend");
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

    private static bool CanTransmitCompetitionData()
    {
        if (FlightBackend is null || !FlightState.IsFresh(Clock.UtcNow, TimeSpan.FromSeconds(5)))
            return false;

        if (AppOptions.GameServer.Environment == CompetitionServerEnvironment.Test)
            return IsLocalMockEndpoint(AppOptions.GameServer.BaseUrl)
                && (FlightBackend.Status is FlightBackendStatus.Live or FlightBackendStatus.Simulation);

        var hasVehiclePosition = FlightState.GpsFix is GpsFix.Fix3D or GpsFix.Dgps or GpsFix.Rtk
            && (Math.Abs(FlightState.Latitude) > 0.000001 || Math.Abs(FlightState.Longitude) > 0.000001);
        return FlightBackend.IsLiveReady
            && FlightState.GpsTimeUtc.HasValue
            && hasVehiclePosition
            && AppOptions.GameServer.TakimNumarasi > 0;
    }

    private static bool IsLocalMockEndpoint(string baseUrl)
        => Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
           && uri.Port == 5000
           && (string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase));

    private static void PublishCompetitionGuardAlert(string action)
    {
        AlertBus.Publish(Alert.Create(
            kind: "competition.guard",
            level: AlertLevel.Warn,
            title: "GÖNDERİM ENGELLENDİ",
            message: $"{action}: resmi sunucuda yalnızca geçerli canlı MAVLink + araç GPS zamanı kabul edilir.",
            timeUtc: Clock.UtcNow));
    }

    private static void PublishCommandFeedback(string message, AlertLevel level)
    {
        AlertBus.Publish(Alert.Create(
            kind: "command.backend",
            level: level,
            title: "UÇUŞ BACKEND",
            message: message,
            timeUtc: Clock.UtcNow));
    }

    /// <summary>
    /// 1 sn debounced settings save — slider gibi sürekli değişen ayarlarda her
    /// hareket için disk I/O yapılmasın diye. Son değişiklikten 1 sn sonra dosyaya yazılır.
    /// </summary>
    private void ScheduleSettingsSave()
    {
        if (_settingsDebounceTimer is null)
        {
            _settingsDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _settingsDebounceTimer.Tick += (_, _) =>
            {
                _settingsDebounceTimer!.Stop();
                try { _settingsStore.Save(AppOptions); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Settings] save failed: {ex.Message}"); }
            };
        }
        _settingsDebounceTimer.Stop();
        _settingsDebounceTimer.Start();
    }

}
