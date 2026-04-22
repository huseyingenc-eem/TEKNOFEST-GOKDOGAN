using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// UYARILAR sekmesi — eşikler (batarya + yakınlık + haberleşme gecikmesi) +
/// sesli uyarılar toggle. Monitor sınıfları (Faz 2) bu değerleri okur.
/// </summary>
public partial class AlertSettingsViewModel : OptionsBackedViewModel<AlertOptions>
{
    public AlertSettingsViewModel() { }

    public AlertSettingsViewModel(AlertOptions options) : base(options)
    {
        _lowBatteryThreshold = options.LowBatteryThreshold;
        _opponentProximityThreshold = options.OpponentProximityThreshold;
        _hssProximityThreshold = options.HssProximityThreshold;
        _boundaryProximityThreshold = options.BoundaryProximityThreshold;
        _commLatencyThreshold = options.CommLatencyThreshold;
        _enableAudioAlerts = options.EnableAudioAlerts;
        _enableLockBeep = options.EnableLockBeep;
    }

    [ObservableProperty] private int _lowBatteryThreshold = 22;
    [ObservableProperty] private double _opponentProximityThreshold = 500;
    [ObservableProperty] private double _hssProximityThreshold = 50;
    [ObservableProperty] private double _boundaryProximityThreshold = 100;
    [ObservableProperty] private int _commLatencyThreshold = 500;
    [ObservableProperty] private bool _enableAudioAlerts = true;
    [ObservableProperty] private bool _enableLockBeep = true;

    partial void OnLowBatteryThresholdChanged(int value) => PushToOptions(o => o.LowBatteryThreshold = value);
    partial void OnOpponentProximityThresholdChanged(double value) => PushToOptions(o => o.OpponentProximityThreshold = value);
    partial void OnHssProximityThresholdChanged(double value) => PushToOptions(o => o.HssProximityThreshold = value);
    partial void OnBoundaryProximityThresholdChanged(double value) => PushToOptions(o => o.BoundaryProximityThreshold = value);
    partial void OnCommLatencyThresholdChanged(int value) => PushToOptions(o => o.CommLatencyThreshold = value);
    partial void OnEnableAudioAlertsChanged(bool value) => PushToOptions(o => o.EnableAudioAlerts = value);
    partial void OnEnableLockBeepChanged(bool value) => PushToOptions(o => o.EnableLockBeep = value);
}
