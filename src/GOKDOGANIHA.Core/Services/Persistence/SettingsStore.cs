using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.Core.Services.Persistence;

/// <summary>
/// ApplicationOptions'ı diskte JSON olarak persiste eder. Yarışma öncesi takım
/// numarası, sunucu URL'i, eşik değerleri tekrar tekrar girilmesin diye.
/// Path: <c>%AppData%\GOKDOGAN\settings.json</c>.
///
/// SOLID: SRP — sadece serialize/deserialize. Caller (App.xaml.cs) Load → Apply →
/// debounced Save flow yönetir. Şifre alanı şu an plain-JSON; üretimde DPAPI ile
/// şifrelenmeli (Windows.Security.DataProtection).
/// </summary>
public sealed class SettingsStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Default path: %AppData%\GOKDOGAN\settings.json</summary>
    public static string DefaultPath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "GOKDOGAN", "settings.json");
        }
    }

    /// <summary>
    /// Diskten yükle. Dosya yoksa default <see cref="ApplicationOptions"/> döner —
    /// caller uygulayarak (PropertyChanged tetikleyerek) UI'a yansıtır.
    /// </summary>
    public ApplicationOptions Load(string? path = null)
    {
        path ??= DefaultPath;
        if (!File.Exists(path)) return new ApplicationOptions();

        try
        {
            var json = File.ReadAllText(path);
            var snapshot = JsonSerializer.Deserialize<OptionsSnapshot>(json, _jsonOptions);
            if (snapshot is null) return new ApplicationOptions();

            var opts = new ApplicationOptions();
            snapshot.ApplyTo(opts);
            return opts;
        }
        catch
        {
            // Bozuk JSON / izin hatası → varsayılana düş (yarışmada bootloop riski yok).
            return new ApplicationOptions();
        }
    }

    /// <summary>Diskte kaydet. Dizin yoksa oluşturur. Atomic değil — basit overwrite.</summary>
    public void Save(ApplicationOptions options, string? path = null)
    {
        path ??= DefaultPath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var snapshot = OptionsSnapshot.From(options);
        var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// JSON şeması — record. Tüm alanlar opsiyonel, geriye dönük uyumlu (yeni alan
    /// eklendiğinde eski settings.json crash etmez). Şifre şu an plain — DPAPI önerilir.
    /// </summary>
    private sealed record OptionsSnapshot(
        string? ServerBaseUrl,
        string? KullaniciAdi,
        string? Sifre,
        int? TakimNumarasi,
        double? TelemetryHz,
        bool? AutoReconnect,
        bool? UseSimulator,
        string? VideoRtspUrl,
        string? MapTileProvider,
        bool? MapShowBoundary,
        bool? MapShowHssZones,
        int? LowBatteryThreshold,
        double? OpponentProximityThreshold,
        double? HssProximityThreshold,
        double? BoundaryProximityThreshold,
        int? CommLatencyThreshold,
        int? GcsTimeoutSeconds,
        CompetitionServerEnvironment? ServerEnvironment,
        string? MavlinkListenAddress,
        int? MavlinkPort,
        int? MavlinkExpectedSystemId,
        double? MavlinkStaleAfterSeconds)
    {
        public void ApplyTo(ApplicationOptions o)
        {
            if (ServerBaseUrl is not null)        o.GameServer.BaseUrl = ServerBaseUrl;
            if (KullaniciAdi is not null)         o.GameServer.KullaniciAdi = KullaniciAdi;
            if (Sifre is not null)                o.GameServer.Sifre = Sifre;
            if (TakimNumarasi.HasValue)           o.GameServer.TakimNumarasi = TakimNumarasi.Value;
            if (TelemetryHz.HasValue)             o.Telemetry.Hz = TelemetryHz.Value;
            if (AutoReconnect.HasValue)           o.Telemetry.AutoReconnect = AutoReconnect.Value;
            // Legacy UseSimulator alanı bilinçli olarak uygulanmaz. Aktif uçuş modu
            // session state'tir ve uygulama açılışında her zaman canlı moddan başlar.
            if (VideoRtspUrl is not null)         o.Video.RtspUrl = VideoRtspUrl;
            if (MapTileProvider is not null)      o.Map.TileProvider = MapTileProvider;
            if (MapShowBoundary.HasValue)         o.Map.ShowBoundary = MapShowBoundary.Value;
            if (MapShowHssZones.HasValue)         o.Map.ShowHssZones = MapShowHssZones.Value;
            if (LowBatteryThreshold.HasValue)     o.Alerts.LowBatteryThreshold = LowBatteryThreshold.Value;
            if (OpponentProximityThreshold.HasValue) o.Alerts.OpponentProximityThreshold = OpponentProximityThreshold.Value;
            if (HssProximityThreshold.HasValue)   o.Alerts.HssProximityThreshold = HssProximityThreshold.Value;
            if (BoundaryProximityThreshold.HasValue) o.Alerts.BoundaryProximityThreshold = BoundaryProximityThreshold.Value;
            if (CommLatencyThreshold.HasValue)    o.Alerts.CommLatencyThreshold = CommLatencyThreshold.Value;
            if (GcsTimeoutSeconds.HasValue)       o.Failsafe.GcsTimeoutSeconds = GcsTimeoutSeconds.Value;
            if (ServerEnvironment.HasValue)       o.GameServer.Environment = ServerEnvironment.Value;
            if (MavlinkListenAddress is not null) o.Mavlink.ListenAddress = MavlinkListenAddress;
            if (MavlinkPort.HasValue)             o.Mavlink.Port = MavlinkPort.Value;
            if (MavlinkExpectedSystemId.HasValue) o.Mavlink.ExpectedSystemId = MavlinkExpectedSystemId.Value;
            if (MavlinkStaleAfterSeconds.HasValue)o.Mavlink.StaleAfterSeconds = MavlinkStaleAfterSeconds.Value;
        }

        public static OptionsSnapshot From(ApplicationOptions o) => new(
            ServerBaseUrl: o.GameServer.BaseUrl,
            KullaniciAdi: o.GameServer.KullaniciAdi,
            Sifre: o.GameServer.Sifre,
            TakimNumarasi: o.GameServer.TakimNumarasi,
            TelemetryHz: o.Telemetry.Hz,
            AutoReconnect: o.Telemetry.AutoReconnect,
            UseSimulator: null,
            VideoRtspUrl: o.Video.RtspUrl,
            MapTileProvider: o.Map.TileProvider,
            MapShowBoundary: o.Map.ShowBoundary,
            MapShowHssZones: o.Map.ShowHssZones,
            LowBatteryThreshold: o.Alerts.LowBatteryThreshold,
            OpponentProximityThreshold: o.Alerts.OpponentProximityThreshold,
            HssProximityThreshold: o.Alerts.HssProximityThreshold,
            BoundaryProximityThreshold: o.Alerts.BoundaryProximityThreshold,
            CommLatencyThreshold: o.Alerts.CommLatencyThreshold,
            GcsTimeoutSeconds: o.Failsafe.GcsTimeoutSeconds,
            ServerEnvironment: o.GameServer.Environment,
            MavlinkListenAddress: o.Mavlink.ListenAddress,
            MavlinkPort: o.Mavlink.Port,
            MavlinkExpectedSystemId: o.Mavlink.ExpectedSystemId,
            MavlinkStaleAfterSeconds: o.Mavlink.StaleAfterSeconds);
    }
}
