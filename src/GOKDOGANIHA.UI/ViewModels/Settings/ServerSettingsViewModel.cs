using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// SUNUCU sekmesi — yarışma API adresi ve takım kimlik bilgileri.
/// GameServerOptions'a çift yönlü bağlanır.
/// </summary>
public partial class ServerSettingsViewModel : OptionsBackedViewModel<GameServerOptions>
{
    private readonly IGameServerClient? _gameServer;

    public ServerSettingsViewModel() { }

    public ServerSettingsViewModel(GameServerOptions options, IGameServerClient? gameServer = null)
        : base(options)
    {
        _gameServer = gameServer;
        _serverBaseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? _serverBaseUrl : options.BaseUrl;
        _teamUsername = string.IsNullOrWhiteSpace(options.KullaniciAdi) ? _teamUsername : options.KullaniciAdi;
        _teamPassword = options.Sifre;
    }

    [ObservableProperty] private string _serverBaseUrl = "http://127.0.0.25:5000";
    [ObservableProperty] private string _teamUsername = "gokdogan";
    [ObservableProperty] private string _teamPassword = string.Empty;

    partial void OnServerBaseUrlChanged(string value) => PushToOptions(o => o.BaseUrl = value);
    partial void OnTeamUsernameChanged(string value) => PushToOptions(o => o.KullaniciAdi = value);
    partial void OnTeamPasswordChanged(string value) => PushToOptions(o => o.Sifre = value);

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (_gameServer is null)
        {
            MessageBox.Show(
                "API istemcisi başlatılmadı.",
                "BAĞLANTI TESTİ",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var response = await _gameServer.GirisAsync();
            MessageBox.Show(
                $"Giriş başarılı.\nTakım numarası: {response.TakimNumarasi}",
                "BAĞLANTI TESTİ",
                MessageBoxButton.OK, MessageBoxImage.Information);
            // OnLoginSucceeded event fire edilirse TeamSettingsViewModel senkron
            // olur — şimdilik GameServerClient zaten _options.TakimNumarasi'na
            // yazdığı için başka VM'ler takip edebilir.
            LoginSucceeded?.Invoke(this, response.TakimNumarasi);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Bağlantı başarısız:\n\n{ex.Message}",
                "BAĞLANTI TESTİ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Login başarılı olunca takım numarasını dinleyiciye verir.</summary>
    public event EventHandler<int>? LoginSucceeded;
}
