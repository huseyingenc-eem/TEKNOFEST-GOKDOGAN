using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Session;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// SUNUCU sekmesi — yarışma API adresi, takım kimlik bilgileri + Test/Connect
/// aksiyonları. GameServerOptions'a çift yönlü bağlanır. Dialog gösterimi için
/// <see cref="IDialogService"/>, login + poll başlatma için
/// <see cref="ConnectionOrchestrator"/> enjekte edilir.
/// </summary>
public partial class ServerSettingsViewModel : OptionsBackedViewModel<GameServerOptions>
{
    private readonly IGameServerClient? _gameServer;
    private readonly IDialogService? _dialog;
    private readonly ConnectionOrchestrator? _orchestrator;

    public ServerSettingsViewModel() { }

    public ServerSettingsViewModel(
        GameServerOptions options,
        IGameServerClient? gameServer = null,
        IDialogService? dialog = null,
        ConnectionOrchestrator? orchestrator = null) : base(options)
    {
        _gameServer = gameServer;
        _dialog = dialog;
        _orchestrator = orchestrator;
        _serverBaseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? _serverBaseUrl : options.BaseUrl;
        _teamUsername = string.IsNullOrWhiteSpace(options.KullaniciAdi) ? _teamUsername : options.KullaniciAdi;
        _teamPassword = options.Sifre;
        _useTestEnvironment = options.Environment == CompetitionServerEnvironment.Test;
        if (_orchestrator is not null)
        {
            _isConnected = _orchestrator.IsConnected;
            _orchestrator.ConnectionStateChanged += OnConnectionStateChanged;
        }
    }

    [ObservableProperty] private string _serverBaseUrl = "http://127.0.0.25:5000";
    [ObservableProperty] private string _teamUsername = "gokdogan";
    [ObservableProperty] private string _teamPassword = string.Empty;
    [ObservableProperty] private bool _useTestEnvironment;

    /// <summary>
    /// Aktif bir bağlantı var mı (ConnectAsync başarılı olmuş). UI butonları
    /// buna göre etkinleşir/devredışı kalır (sonraki iterasyon).
    /// </summary>
    [ObservableProperty] private bool _isConnected;

    partial void OnServerBaseUrlChanged(string value) => PushToOptions(o => o.BaseUrl = value);
    partial void OnTeamUsernameChanged(string value) => PushToOptions(o => o.KullaniciAdi = value);
    partial void OnTeamPasswordChanged(string value) => PushToOptions(o => o.Sifre = value);
    partial void OnUseTestEnvironmentChanged(bool value)
    {
        PushToOptions(o => o.Environment = value
            ? CompetitionServerEnvironment.Test
            : CompetitionServerEnvironment.Official);

        // Yerel mock profili yanlışlıkla resmî URL ile kullanılmasın.
        if (value && ServerBaseUrl == "http://127.0.0.25:5000")
            ServerBaseUrl = "http://127.0.0.1:5000";
        else if (!value && ServerBaseUrl == "http://127.0.0.1:5000")
            ServerBaseUrl = "http://127.0.0.25:5000";
    }

    /// <summary>Sadece login atar — poll başlatmaz (hızlı kimlik doğrulama).</summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (_gameServer is null)
        {
            if (_dialog is not null)
                await _dialog.ShowWarnAsync("BAĞLANTI TESTİ", "API istemcisi başlatılmadı.");
            return;
        }

        try
        {
            var response = await _gameServer.GirisAsync();
            LoginSucceeded?.Invoke(this, response.TakimNumarasi);
            if (_dialog is not null)
                await _dialog.ShowInfoAsync("BAĞLANTI TESTİ",
                    $"Giriş başarılı.\nTakım numarası: {response.TakimNumarasi}");
        }
        catch (Exception ex)
        {
            if (_dialog is not null)
                await _dialog.ShowErrorAsync("BAĞLANTI TESTİ", $"Bağlantı başarısız:\n\n{ex.Message}");
        }
    }

    /// <summary>
    /// Tam bağlantı: Login + Telemetri/HSS poll servislerini başlatır. Yarışma
    /// akışının giriş noktası.
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (_orchestrator is null)
        {
            if (_dialog is not null)
                await _dialog.ShowWarnAsync("BAĞLAN", "Bağlantı orkestratörü başlatılmadı.");
            return;
        }

        var ok = await _orchestrator.ConnectAsync();
        IsConnected = ok;
        if (_dialog is null) return;
        if (ok)
            await _dialog.ShowInfoAsync("BAĞLAN",
                "Yarışma sunucusuna bağlanıldı. Telemetri gönderimi başladı.\n\nÜst çubuktaki SUNUCU göstergesi artık bağlantı durumunu ve kopma olursa otomatik yeniden denemeleri gösterir.");
        else
            await _dialog.ShowErrorAsync("BAĞLAN",
                "Bağlantı kurulamadı. Üst çubuktaki SUNUCU göstergesinde 'Tekrar Dene' ile yeniden deneyebilir, ayrıntı için Olay Günlüğü'ne bakabilirsiniz.");
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        if (_orchestrator is null) return;
        await _orchestrator.DisconnectAsync();
        IsConnected = false;
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) IsConnected = connected;
        else dispatcher.BeginInvoke((Action)(() => IsConnected = connected));
    }

    /// <summary>Login başarılı olunca takım numarasını dinleyiciye verir.</summary>
    public event EventHandler<int>? LoginSucceeded;
}
