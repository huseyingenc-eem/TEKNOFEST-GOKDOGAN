using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Session;
using System;
using System.Threading.Tasks;

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
    }

    [ObservableProperty] private string _serverBaseUrl = "http://127.0.0.25:5000";
    [ObservableProperty] private string _teamUsername = "gokdogan";
    [ObservableProperty] private string _teamPassword = string.Empty;

    /// <summary>
    /// Aktif bir bağlantı var mı (ConnectAsync başarılı olmuş). UI butonları
    /// buna göre etkinleşir/devredışı kalır (sonraki iterasyon).
    /// </summary>
    [ObservableProperty] private bool _isConnected;

    partial void OnServerBaseUrlChanged(string value) => PushToOptions(o => o.BaseUrl = value);
    partial void OnTeamUsernameChanged(string value) => PushToOptions(o => o.KullaniciAdi = value);
    partial void OnTeamPasswordChanged(string value) => PushToOptions(o => o.Sifre = value);

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
        if (!ok && _dialog is not null)
            await _dialog.ShowErrorAsync("BAĞLAN",
                "Bağlantı kurulamadı. AlertConsole'daki detaylara bakın.");
    }

    /// <summary>Login başarılı olunca takım numarasını dinleyiciye verir.</summary>
    public event EventHandler<int>? LoginSucceeded;
}
