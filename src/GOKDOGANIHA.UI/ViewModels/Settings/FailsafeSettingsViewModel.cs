using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

public partial class FailsafeSettingsViewModel : OptionsBackedViewModel<FailsafeOptions>
{
    public FailsafeSettingsViewModel() { }

    public FailsafeSettingsViewModel(FailsafeOptions options) : base(options)
    {
        _gcsTimeoutSeconds = options.GcsTimeoutSeconds;
        _batteryRtlPercent = options.BatteryRtlPercent;
        _gpsLoss = options.GpsLoss;
    }

    [ObservableProperty] private int _gcsTimeoutSeconds = 10;
    [ObservableProperty] private int _batteryRtlPercent = 20;
    [ObservableProperty] private GpsLossBehavior _gpsLoss = GpsLossBehavior.Land;

    partial void OnGcsTimeoutSecondsChanged(int value) => PushToOptions(o => o.GcsTimeoutSeconds = value);
    partial void OnBatteryRtlPercentChanged(int value) => PushToOptions(o => o.BatteryRtlPercent = value);
    partial void OnGpsLossChanged(GpsLossBehavior value) => PushToOptions(o => o.GpsLoss = value);
}
