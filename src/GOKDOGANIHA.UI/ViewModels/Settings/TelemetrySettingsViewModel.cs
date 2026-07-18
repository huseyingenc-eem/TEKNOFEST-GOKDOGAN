using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Telemetry;
using System.ComponentModel;
using System.IO.Ports;
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
    private DateTime _lastSerialPortRefreshUtc = DateTime.MinValue;

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
            _mavlinkTransport = mavlinkOptions.Transport;
            _mavlinkListenAddress = mavlinkOptions.ListenAddress;
            _mavlinkPort = mavlinkOptions.Port;
            _mavlinkSerialPortName = mavlinkOptions.SerialPortName;
            _mavlinkBaudRate = mavlinkOptions.BaudRate;
            _mavlinkExpectedSystemId = mavlinkOptions.ExpectedSystemId;
            _mavlinkStaleAfterSeconds = mavlinkOptions.StaleAfterSeconds;
            _mavlinkAutoReconnect = mavlinkOptions.AutoReconnect;
        }
        RefreshSerialPorts();
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
    [ObservableProperty] private MavlinkTransport _mavlinkTransport = MavlinkTransport.Udp;
    [ObservableProperty] private string _mavlinkListenAddress = "0.0.0.0";
    [ObservableProperty] private int _mavlinkPort = 14550;
    [ObservableProperty] private string _mavlinkSerialPortName = "COM3";
    [ObservableProperty] private int _mavlinkBaudRate = 57600;
    [ObservableProperty] private int _mavlinkExpectedSystemId;
    [ObservableProperty] private double _mavlinkStaleAfterSeconds = 3;
    [ObservableProperty] private bool _mavlinkAutoReconnect = true;
    [ObservableProperty] private IReadOnlyList<string> _availableSerialPorts = Array.Empty<string>();

    public IReadOnlyList<MavlinkTransport> MavlinkTransportOptions { get; }
        = Enum.GetValues<MavlinkTransport>();

    public IReadOnlyList<int> MavlinkBaudRateOptions { get; }
        = new[] { 57600, 115200, 230400, 460800, 921600 };

    public string MavlinkConnectionSummary => MavlinkTransport == GOKDOGANIHA.Core.Configuration.MavlinkTransport.Serial
        ? $"Seri {MavlinkSerialPortName} @ {MavlinkBaudRate} baud (8N1)"
        : $"UDP {MavlinkListenAddress}:{MavlinkPort}";
    public bool IsUdpMavlinkTransport => MavlinkTransport == GOKDOGANIHA.Core.Configuration.MavlinkTransport.Udp;
    public bool IsSerialMavlinkTransport => MavlinkTransport == GOKDOGANIHA.Core.Configuration.MavlinkTransport.Serial;
    public bool IsSelectedSerialPortAvailable => IsSerialMavlinkTransport
        && !string.IsNullOrWhiteSpace(MavlinkSerialPortName)
        && AvailableSerialPorts.Contains(MavlinkSerialPortName, StringComparer.OrdinalIgnoreCase);
    public string SerialPortStatusText
    {
        get
        {
            if (!IsSerialMavlinkTransport) return string.Empty;
            if (IsSelectedSerialPortAvailable)
                return $"HAZIR · {MavlinkSerialPortName} bu bilgisayarda mevcut";
            if (AvailableSerialPorts.Count == 0)
                return MavlinkAutoReconnect
                    ? "RFD/seri cihaz bulunamadı. Otomatik bağlantı USB'nin takılmasını bekliyor."
                    : "RFD/seri cihaz bulunamadı. USB'yi bağlayıp YENİLE'ye basın.";
            return MavlinkAutoReconnect
                ? $"{MavlinkSerialPortName} mevcut değil. Otomatik bağlantı portu bekliyor."
                : $"{MavlinkSerialPortName} mevcut değil. USB'yi bağlayıp YENİLE'ye basın.";
        }
    }

    partial void OnHzChanged(double value) => PushToOptions(o => o.Hz = value);
    partial void OnAutoReconnectChanged(bool value) => PushToOptions(o => o.AutoReconnect = value);
    partial void OnMavlinkTransportChanged(MavlinkTransport value)
    {
        if (_mavlinkOptions is not null) _mavlinkOptions.Transport = value;
        OnPropertyChanged(nameof(MavlinkConnectionSummary));
        OnPropertyChanged(nameof(IsUdpMavlinkTransport));
        OnPropertyChanged(nameof(IsSerialMavlinkTransport));
        OnPropertyChanged(nameof(IsSelectedSerialPortAvailable));
        OnPropertyChanged(nameof(SerialPortStatusText));
    }
    partial void OnMavlinkListenAddressChanged(string value)
    {
        if (_mavlinkOptions is not null) _mavlinkOptions.ListenAddress = value;
        OnPropertyChanged(nameof(MavlinkConnectionSummary));
    }
    partial void OnMavlinkPortChanged(int value)
    {
        if (_mavlinkOptions is not null) _mavlinkOptions.Port = value;
        OnPropertyChanged(nameof(MavlinkConnectionSummary));
    }
    partial void OnMavlinkSerialPortNameChanged(string value)
    {
        if (_mavlinkOptions is not null) _mavlinkOptions.SerialPortName = value;
        OnPropertyChanged(nameof(MavlinkConnectionSummary));
        OnPropertyChanged(nameof(IsSelectedSerialPortAvailable));
        OnPropertyChanged(nameof(SerialPortStatusText));
    }
    partial void OnMavlinkBaudRateChanged(int value)
    {
        if (_mavlinkOptions is not null) _mavlinkOptions.BaudRate = value;
        OnPropertyChanged(nameof(MavlinkConnectionSummary));
    }
    partial void OnMavlinkExpectedSystemIdChanged(int value) { if (_mavlinkOptions is not null) _mavlinkOptions.ExpectedSystemId = value; }
    partial void OnMavlinkStaleAfterSecondsChanged(double value) { if (_mavlinkOptions is not null) _mavlinkOptions.StaleAfterSeconds = value; }
    partial void OnMavlinkAutoReconnectChanged(bool value)
    {
        if (_mavlinkOptions is not null) _mavlinkOptions.AutoReconnect = value;
        OnPropertyChanged(nameof(SerialPortStatusText));
    }
    partial void OnAvailableSerialPortsChanged(IReadOnlyList<string> value)
    {
        OnPropertyChanged(nameof(IsSelectedSerialPortAvailable));
        OnPropertyChanged(nameof(SerialPortStatusText));
    }

    [RelayCommand]
    private void RefreshSerialPorts()
        => RefreshSerialPortsCore(force: true);

    private void RefreshSerialPortsCore(bool force)
    {
        var nowUtc = DateTime.UtcNow;
        if (!force && nowUtc - _lastSerialPortRefreshUtc < TimeSpan.FromMilliseconds(500))
            return;
        _lastSerialPortRefreshUtc = nowUtc;

        try
        {
            AvailableSerialPorts = SerialPort.GetPortNames()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            AvailableSerialPorts = Array.Empty<string>();
        }
    }

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
                : $"Simülasyon durdurulacak ve canlı MAVLink kaynağına bağlanılacaktır.\n\n{MavlinkConnectionSummary}\nİlk geçerli HEARTBEAT alınmadan sistem CANLI sayılmayacaktır.\n\nCanlı veriye geçilsin mi?",
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
        if (IsSerialMavlinkTransport)
        {
            RefreshSerialPorts();
            if (!IsSelectedSerialPortAvailable)
            {
                var available = AvailableSerialPorts.Count == 0
                    ? "Hiç seri port bulunamadı."
                    : $"Görünen portlar: {string.Join(", ", AvailableSerialPorts)}";
                await _dialog.ShowErrorAsync(
                    "RFD SERİ PORTU BULUNAMADI",
                    $"Seçili {MavlinkSerialPortName} bu bilgisayarda mevcut değil. " +
                    "RFD yer modemini USB ile bağlayın, YENİLE'ye basın ve oluşan yeni COM portunu seçin.\n\n" +
                    available);
                return;
            }
        }

        var confirmed = await _dialog.ConfirmAsync(
            "MAVLINK BAĞLANTISINI YENİLE",
            $"Aktif uçuş kaynağı durdurulup yeni ayarlarla yeniden başlatılacaktır.\n\n{MavlinkConnectionSummary}\nSystem ID: {MavlinkExpectedSystemId}\n\nDevam edilsin mi?",
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
            else
                await _dialog.ShowInfoAsync("MAVLINK BAĞLANTISI",
                    $"{MavlinkConnectionSummary} açıldı.\n\nİlk geçerli HEARTBEAT gelince üst çubuktaki TELEMETRİ göstergesi CANLI'ya döner. Veri gelmiyorsa karşı tarafın (Pixhawk/RFD) açık ve bağlı olduğundan emin olun.");
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
            if (IsSerialMavlinkTransport)
                RefreshSerialPortsCore(force: false);
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
