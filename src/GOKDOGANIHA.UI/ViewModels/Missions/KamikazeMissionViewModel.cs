using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Alerts;
using GOKDOGANIHA.Core.Services.Autonomy;
using GOKDOGANIHA.Core.Services.Session;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.UI.Services;
using GOKDOGANIHA.UI.ViewModels.Shell;

namespace GOKDOGANIHA.UI.ViewModels.Missions;

public partial class KamikazeMissionViewModel : ObservableObject, IDisposable
{
    private readonly KamikazeFsm? _mission;
    private readonly FlightState? _flightState;
    private readonly FlightBackendCoordinator? _backend;
    private readonly ConnectionOrchestrator? _connection;
    private readonly IDialogService _dialogs;
    private readonly AlertBus _alerts;
    private readonly IClock _clock;
    private readonly MapViewModel _map;
    private readonly OverlayViewModel _overlay;

    internal KamikazeMissionViewModel(
        KamikazeFsm? mission,
        FlightState? flightState,
        FlightBackendCoordinator? backend,
        ConnectionOrchestrator? connection,
        IDialogService dialogs,
        AlertBus alerts,
        IClock clock,
        MapViewModel map,
        OverlayViewModel overlay)
    {
        _mission = mission;
        _flightState = flightState;
        _backend = backend;
        _connection = connection;
        _dialogs = dialogs;
        _alerts = alerts;
        _clock = clock;
        _map = map;
        _overlay = overlay;

        if (_backend is not null)
            _backend.PropertyChanged += OnBackendChanged;
        if (_connection is not null)
            _connection.QrCoordinateReceived += OnQrCoordinateReceived;

        ApplySimulationTarget();
    }

    [ObservableProperty] private KamikazePhase _phase = KamikazePhase.Idle;
    [ObservableProperty] private string _status = "Görev pasif";
    [ObservableProperty] private string _qrText = string.Empty;
    [ObservableProperty] private int _attempt;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private string _elapsed = "—";

    public void Refresh(DateTime nowUtc)
    {
        if (_mission is null) return;

        if (_flightState is not null)
            _mission.Tick(_flightState);

        Phase = _mission.Phase;
        Status = _mission.StatusMessage;
        QrText = _mission.QrText;
        Attempt = _mission.AttemptCount;

        if (_mission.MissionStartUtc is { } start && _mission.IsActive)
        {
            var span = nowUtc - start;
            Elapsed = $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
        }
        else if (!_mission.IsActive && _mission.Phase is KamikazePhase.Idle)
        {
            Elapsed = "—";
        }

        if (_mission.IsActive == IsActive) return;
        IsActive = _mission.IsActive;
        _overlay.IsKamikazeFullscreen = IsActive;
    }

    public void Dispose()
    {
        if (_backend is not null)
            _backend.PropertyChanged -= OnBackendChanged;
        if (_connection is not null)
            _connection.QrCoordinateReceived -= OnQrCoordinateReceived;
    }

    [RelayCommand]
    private async Task Start()
    {
        var target = _map.QrTarget;
        if (target is null)
        {
            if (_backend?.IsSimulationActive != true)
            {
                await _dialogs.ShowErrorAsync(
                    "KAMİKAZE BAŞLATILAMADI",
                    "Canlı modda QR koordinatı alınmadan görev başlatılamaz.\n\nÖnce yarışma sunucusundan geçerli QR koordinatını alın.");
                return;
            }

            target = SimulationDefaults.QrTarget;
            _map.QrTarget = target;
            _alerts.Publish(Alert.Create(
                kind: "kamikaze.start-mock",
                level: AlertLevel.Warn,
                title: "KAMİKAZE — MOCK QR",
                message: "Sunucu QR koordinatı yok; test için sahte koordinat kullanılıyor.",
                timeUtc: _clock.UtcNow));
        }

        _mission?.StartMission(target.Enlem, target.Boylam);
    }

    [RelayCommand]
    private void Abort() => _mission?.Abort("Kullanıcı iptali");

    [RelayCommand]
    private void SimulateQrRead() => _mission?.ApplyQrRead("MOCK-QR-12345");

    private void OnBackendChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(FlightBackendCoordinator.ActiveMode)
            or nameof(FlightBackendCoordinator.Status))) return;

        Dispatch(ApplySimulationTarget);
    }

    private void OnQrCoordinateReceived(object? sender, QrKoordinat coordinate)
    {
        if (_backend?.IsSimulationActive == true) return;
        Dispatch(() => _map.QrTarget = coordinate);
    }

    private void ApplySimulationTarget()
        => _map.QrTarget = _backend?.IsSimulationActive == true
            ? SimulationDefaults.QrTarget
            : null;

    private static void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }
}
