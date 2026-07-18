using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Session;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Services;

public interface ISettingsViewModelFactory
{
    SettingsViewModel Create();
}

public sealed class SettingsViewModelFactory : ISettingsViewModelFactory
{
    private readonly ApplicationOptions _options;
    private readonly IGameServerClient? _gameServer;
    private readonly IDialogService? _dialog;
    private readonly ConnectionOrchestrator? _orchestrator;
    private readonly FlightBackendCoordinator? _flightBackend;

    public SettingsViewModelFactory(
        ApplicationOptions options,
        IGameServerClient? gameServer = null,
        IDialogService? dialog = null,
        ConnectionOrchestrator? orchestrator = null,
        FlightBackendCoordinator? flightBackend = null)
    {
        _options = options;
        _gameServer = gameServer;
        _dialog = dialog;
        _orchestrator = orchestrator;
        _flightBackend = flightBackend;
    }

    public SettingsViewModel Create() => new(_options, _gameServer, _dialog, _orchestrator, _flightBackend);
}
