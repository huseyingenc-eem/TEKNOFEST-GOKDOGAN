using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;

namespace GOKDOGANIHA.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    // Live GameServerOptions the VM writes to on every setter change.
    // Null in design-time / parameterless-ctor path — in that case setters
    // just stay local and never reach any service.
    private readonly GameServerOptions? _serverOptions;
    private readonly IGameServerClient? _gameServer;

    public SettingsViewModel() { }

    public SettingsViewModel(GameServerOptions options, IGameServerClient? gameServer = null)
    {
        _serverOptions = options;
        _gameServer = gameServer;

        // Seed VM from the authoritative options object so user sees the
        // same values the service is actually using.
        _serverBaseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? _serverBaseUrl : options.BaseUrl;
        _teamUsername = string.IsNullOrWhiteSpace(options.KullaniciAdi) ? _teamUsername : options.KullaniciAdi;
        _teamPassword = options.Sifre;
        _teamNumber = options.TakimNumarasi;
    }

    // ============ SUNUCU ============
    [ObservableProperty] private string _serverBaseUrl = "http://127.0.0.25:5000";
    [ObservableProperty] private string _teamUsername = "gokdogan";
    [ObservableProperty] private string _teamPassword = string.Empty;

    partial void OnServerBaseUrlChanged(string value)
    {
        if (_serverOptions is not null) _serverOptions.BaseUrl = value;
    }

    partial void OnTeamUsernameChanged(string value)
    {
        if (_serverOptions is not null) _serverOptions.KullaniciAdi = value;
    }

    partial void OnTeamPasswordChanged(string value)
    {
        if (_serverOptions is not null) _serverOptions.Sifre = value;
    }

    // ============ TAKIM ============
    [ObservableProperty] private int _teamNumber;
    [ObservableProperty] private string _callSign = "GÖKDOĞAN-1";

    partial void OnTeamNumberChanged(int value)
    {
        if (_serverOptions is not null) _serverOptions.TakimNumarasi = value;
    }

    // ============ TELEMETRİ ============
    [ObservableProperty] private double _telemetryHz = 1.0;
    [ObservableProperty] private bool _autoReconnect = true;

    // ============ VİDEO ============
    [ObservableProperty] private string _videoRtspUrl = "rtsp://192.168.1.100:554/stream";
    [ObservableProperty] private string _videoPreset = "1080p/30";

    // ============ HARİTA ============
    [ObservableProperty] private string _mapTileProvider = "OpenStreetMap";
    [ObservableProperty] private bool _showGrid = true;
    [ObservableProperty] private bool _showBoundary = true;
    [ObservableProperty] private bool _showHssZones = true;

    // ============ UYARILAR ============
    [ObservableProperty] private bool _enableAudioAlerts = true;
    [ObservableProperty] private bool _enableLockBeep = true;
    [ObservableProperty] private int _lowBatteryThreshold = 22;

    // Yakınlık eşikleri — AlertService bunları takip edecek (sonraki iterasyon)
    [ObservableProperty] private double _opponentProximityThreshold = 500;
    [ObservableProperty] private double _hssProximityThreshold = 50;
    [ObservableProperty] private double _boundaryProximityThreshold = 100;
    [ObservableProperty] private int _commLatencyThreshold = 500;

    // ============ COMMANDS ============

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (_gameServer is null)
        {
            MessageBox.Show(
                "API istemcisi başlatılmadı (App servisi null). Uygulama henüz oturum açma için hazır değil.",
                "BAĞLANTI TESTİ",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var response = await _gameServer.GirisAsync();
            // GameServerClient already wrote response.TakimNumarasi into _options —
            // mirror it back into the VM from the response itself (not from options)
            // so the binding updates even if the wire path is temporarily out of
            // sync. Assigning TeamNumber re-triggers OnTeamNumberChanged which
            // writes to options again (idempotent — same value).
            TeamNumber = response.TakimNumarasi;

            MessageBox.Show(
                $"Giriş başarılı.\nTakım numarası: {response.TakimNumarasi}",
                "BAĞLANTI TESTİ",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Bağlantı başarısız:\n\n{ex.Message}",
                "BAĞLANTI TESTİ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
