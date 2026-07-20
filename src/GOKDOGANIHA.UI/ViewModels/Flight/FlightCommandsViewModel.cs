using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Services.Alerts;
using GOKDOGANIHA.UI.ViewModels;
using FlightModeEnum = GOKDOGANIHA.Core.Models.FlightMode;

namespace GOKDOGANIHA.UI.ViewModels.Flight;

public partial class FlightCommandsViewModel
{
    private readonly IFlightCommandSink? _commands;
    private readonly IDialogService _dialogs;
    private readonly AlertBus _alerts;
    private readonly IClock _clock;
    private readonly FlightTelemetryViewModel _flight;
    private readonly MapViewModel _map;

    internal FlightCommandsViewModel(
        IFlightCommandSink? commands,
        IDialogService dialogs,
        AlertBus alerts,
        IClock clock,
        FlightTelemetryViewModel flight,
        MapViewModel map)
    {
        _commands = commands;
        _dialogs = dialogs;
        _alerts = alerts;
        _clock = clock;
        _flight = flight;
        _map = map;
    }

    [RelayCommand]
    private async Task Arm()
    {
        if (!_flight.IsArmed)
        {
            _commands?.Arm();
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            title: "DISARM ONAYI",
            message: "Motorlar durdurulacak. Uçak havadaysa düşecektir.\n\nDevam edilsin mi?",
            yesText: "Evet, DISARM",
            noText: "İptal");
        if (confirmed) _commands?.Disarm();
    }

    [RelayCommand]
    private void SetMode(string? mode)
    {
        if (string.IsNullOrEmpty(mode)) return;
        if (Enum.TryParse<FlightModeEnum>(mode, ignoreCase: true, out var flightMode))
            _commands?.SetMode(flightMode);
    }

    [RelayCommand] private void Rtl() => _commands?.Rtl();
    [RelayCommand] private void Land() => _commands?.Land();
    [RelayCommand] private void Loiter() => _commands?.Loiter();

    [RelayCommand]
    private void ToggleAutonomous()
        => _commands?.SetMode(_flight.IsAutonomous ? FlightModeEnum.Manual : FlightModeEnum.Auto);

    [RelayCommand]
    private void SelectTarget()
    {
        _alerts.Publish(Alert.Create(
            kind: "command.target-select",
            level: AlertLevel.Info,
            title: "HEDEF SEÇ",
            message: "Hedef seçim diyaloğu — KilitlenmeDenetim entegrasyonu",
            timeUtc: _clock.UtcNow));
    }

    [RelayCommand]
    private void GotoWaypoint()
    {
        var first = _map.Waypoints.Count > 0 ? _map.Waypoints[0] : null;
        if (first is null)
        {
            _alerts.Publish(Alert.Create(
                kind: "command.goto-empty",
                level: AlertLevel.Warn,
                title: "WAYPOINT GİT",
                message: "Henüz tanımlı waypoint yok — önce harita üzerinden ekleyin",
                timeUtc: _clock.UtcNow));
            return;
        }

        _commands?.GotoWaypoint(first.Latitude, first.Longitude, first.AltitudeMeters);
    }
}
