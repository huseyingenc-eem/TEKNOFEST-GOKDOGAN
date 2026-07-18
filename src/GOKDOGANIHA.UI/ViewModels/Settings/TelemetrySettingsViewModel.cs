using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Telemetry;
using System.ComponentModel;
using System.Windows;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// TELEMETRİ sekmesi — paket gönderim Hz + otomatik reconnect.
/// </summary>
public partial class TelemetrySettingsViewModel : OptionsBackedViewModel<TelemetryOptions>
{
    private readonly MavlinkOptions? _mavlinkOptions;
    private readonly FlightBackendCoordinator? _backend;
    private readonly IDialogService? _dialog;

    public TelemetrySettingsViewModel() { }

    public TelemetrySettingsViewModel(
        TelemetryOptions options,
        MavlinkOptions? mavlinkOptions = null,
        FlightBackendCoordinator? backend = null,
        IDialogService? dialog = null) : base(options)
    {
        _mavlinkOptions = mavlinkOptions;
        _backend = backend;
        _dialog = dialog;
        _hz = options.Hz;
        _autoReconnect = options.AutoReconnect;
        if (mavlinkOptions is not null)
        {
            _mavlinkListenAddress = mavlinkOptions.ListenAddress;
            _mavlinkPort = mavlinkOptions.Port;
            _mavlinkExpectedSystemId = mavlinkOptions.ExpectedSystemId;
            _mavlinkStaleAfterSeconds = mavlinkOptions.StaleAfterSeconds;
        }
        if (_backend is not null)
        {
            _backend.PropertyChanged += OnBackendPropertyChanged;
            SyncBackendState();
        }
    }

    [ObservableProperty] private double _hz = 1.0;
    [ObservableProperty] private bool _autoReconnect = true;
    [ObservableProperty] private bool _isSimulationActive;
    [ObservableProperty] private bool _isModeSwitchBusy;
    [ObservableProperty] private string _flightBackendStatusText = "CANLI · BAĞLANTI BEKLENİYOR";
    [ObservableProperty] private string _mavlinkListenAddress = "0.0.0.0";
    [ObservableProperty] private int _mavlinkPort = 14550;
    [ObservableProperty] private int _mavlinkExpectedSystemId;
    [ObservableProperty] private double _mavlinkStaleAfterSeconds = 3;

    partial void OnHzChanged(double value) => PushToOptions(o => o.Hz = value);
    partial void OnAutoReconnectChanged(bool value) => PushToOptions(o => o.AutoReconnect = value);
    partial void OnMavlinkListenAddressChanged(string value) { if (_mavlinkOptions is not null) _mavlinkOptions.ListenAddress = value; }
    partial void OnMavlinkPortChanged(int value) { if (_mavlinkOptions is not null) _mavlinkOptions.Port = value; }
    partial void OnMavlinkExpectedSystemIdChanged(int value) { if (_mavlinkOptions is not null) _mavlinkOptions.ExpectedSystemId = value; }
    partial void OnMavlinkStaleAfterSecondsChanged(double value) { if (_mavlinkOptions is not null) _mavlinkOptions.StaleAfterSeconds = value; }

    [RelayCommand]
    private async Task ToggleSimulationAsync()
    {
        if (_backend is null || _dialog is null) return;
        var target = _backend.IsSimulationActive ? FlightDataMode.Live : FlightDataMode.Simulation;
        var isSimulation = target == FlightDataMode.Simulation;
        var confirmed = await _dialog.ConfirmAsync(
            isSimulation ? "SİMÜLASYON MODUNA GEÇ" : "CANLI VERİYE GEÇ",
            isSimulation
                ? "Ekrandaki uçuş telemetrisi sahte verilerle değiştirilecektir.\n\nGerçek araca gönderilen komutlar devre dışı kalacak ve resmi yarışma sunucusuna simülasyon telemetrisi gönderilmeyecektir.\n\nSimülasyon moduna geçilsin mi?"
                : $"Simülasyon durdurulacak ve canlı MAVLink kaynağına bağlanılacaktır.\n\nUDP: {MavlinkListenAddress}:{MavlinkPort}\nİlk geçerli HEARTBEAT alınmadan sistem CANLI sayılmayacaktır.\n\nCanlı veriye geçilsin mi?",
            isSimulation ? "Simülasyonu Aç" : "Canlıya Geç",
            "İptal");
        if (!confirmed) return;

        IsModeSwitchBusy = true;
        try
        {
            var result = await _backend.SwitchAsync(target);
            SyncBackendState();
            if (!result.Success)
                await _dialog.ShowErrorAsync("VERİ KAYNAĞI", result.Message);
        }
        finally
        {
            IsModeSwitchBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReconnectLiveAsync()
    {
        if (_backend is null || _dialog is null) return;
        var confirmed = await _dialog.ConfirmAsync(
            "MAVLINK BAĞLANTISINI YENİLE",
            $"Aktif uçuş kaynağı durdurulup yeni ayarlarla yeniden başlatılacaktır.\n\nUDP: {MavlinkListenAddress}:{MavlinkPort}\nSystem ID: {MavlinkExpectedSystemId}\n\nDevam edilsin mi?",
            "Yeniden Bağlan",
            "İptal");
        if (!confirmed) return;

        IsModeSwitchBusy = true;
        try
        {
            var result = await _backend.SwitchAsync(FlightDataMode.Live, forceRestart: true);
            SyncBackendState();
            if (!result.Success)
                await _dialog.ShowErrorAsync("MAVLINK", result.Message);
        }
        finally
        {
            IsModeSwitchBusy = false;
        }
    }

    private void OnBackendPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => SyncBackendState();

    private void SyncBackendState()
    {
        if (_backend is null) return;
        void Apply()
        {
            IsSimulationActive = _backend.IsSimulationActive;
            FlightBackendStatusText = $"{FormatStatus(_backend.Status)} · {_backend.StatusMessage}";
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) Apply();
        else dispatcher.BeginInvoke((Action)Apply);
    }

    private static string FormatStatus(FlightBackendStatus status) => status switch
    {
        FlightBackendStatus.Live => "CANLI",
        FlightBackendStatus.Simulation => "SİMÜLASYON",
        FlightBackendStatus.ConnectingLive => "CANLI BEKLENİYOR",
        FlightBackendStatus.StartingSimulation => "SİMÜLASYON BAŞLIYOR",
        FlightBackendStatus.Switching => "GEÇİŞ",
        FlightBackendStatus.Faulted => "HATA",
        _ => "BAĞLANTISIZ"
    };
}
