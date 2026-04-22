using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// TELEMETRİ sekmesi — paket gönderim Hz + otomatik reconnect.
/// </summary>
public partial class TelemetrySettingsViewModel : OptionsBackedViewModel<TelemetryOptions>
{
    public TelemetrySettingsViewModel() { }

    public TelemetrySettingsViewModel(TelemetryOptions options) : base(options)
    {
        _hz = options.Hz;
        _autoReconnect = options.AutoReconnect;
    }

    [ObservableProperty] private double _hz = 1.0;
    [ObservableProperty] private bool _autoReconnect = true;

    partial void OnHzChanged(double value) => PushToOptions(o => o.Hz = value);
    partial void OnAutoReconnectChanged(bool value) => PushToOptions(o => o.AutoReconnect = value);
}
