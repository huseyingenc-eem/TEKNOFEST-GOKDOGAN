using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Services;

/// <summary>
/// <see cref="SettingsViewModel"/>'in runtime bağımlılıklarını (options + gameServer + dialog)
/// sarmalayıp statik App coupling'ini izole eder. MainWindowViewModel bunu enjekte alabilir.
/// </summary>
public interface ISettingsViewModelFactory
{
    SettingsViewModel Create();
}

public sealed class SettingsViewModelFactory : ISettingsViewModelFactory
{
    private readonly ApplicationOptions _options;
    private readonly IGameServerClient? _gameServer;
    private readonly IDialogService? _dialog;

    public SettingsViewModelFactory(
        ApplicationOptions options,
        IGameServerClient? gameServer = null,
        IDialogService? dialog = null)
    {
        _options = options;
        _gameServer = gameServer;
        _dialog = dialog;
    }

    public SettingsViewModel Create() => new(_options, _gameServer, _dialog);
}
