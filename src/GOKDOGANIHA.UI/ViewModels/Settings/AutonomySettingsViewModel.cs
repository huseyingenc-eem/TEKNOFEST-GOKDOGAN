using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

public partial class AutonomySettingsViewModel : OptionsBackedViewModel<AutonomyOptions>
{
    public AutonomySettingsViewModel() { }

    public AutonomySettingsViewModel(AutonomyOptions options) : base(options)
    {
        _weightDistance = options.WeightDistance;
        _weightAngle = options.WeightAngle;
        _weightHistory = options.WeightHistory;
        _weightRisk = options.WeightRisk;
        _lockTolerancePercent = options.LockTolerancePercent;
        _lockWindowSeconds = options.LockWindowSeconds;
        _lockRequiredSeconds = options.LockRequiredSeconds;
        _kamikazeApproachAltitude = options.KamikazeApproachAltitudeMeters;
        _kamikazeDiveAngle = options.KamikazeDiveAngleDegrees;
        _kamikazePullUpAltitude = options.KamikazePullUpAltitudeMeters;
        _kamikazeMaxAttempts = options.KamikazeMaxAttempts;
    }

    [ObservableProperty] private double _weightDistance = 0.4;
    [ObservableProperty] private double _weightAngle = 0.3;
    [ObservableProperty] private double _weightHistory = 0.2;
    [ObservableProperty] private double _weightRisk = 0.1;
    [ObservableProperty] private double _lockTolerancePercent = 6;
    [ObservableProperty] private double _lockWindowSeconds = 5;
    [ObservableProperty] private double _lockRequiredSeconds = 4;
    [ObservableProperty] private double _kamikazeApproachAltitude = 100;
    [ObservableProperty] private double _kamikazeDiveAngle = 45;
    [ObservableProperty] private double _kamikazePullUpAltitude = 30;
    [ObservableProperty] private int _kamikazeMaxAttempts = 2;

    partial void OnWeightDistanceChanged(double value) => PushToOptions(o => o.WeightDistance = value);
    partial void OnWeightAngleChanged(double value) => PushToOptions(o => o.WeightAngle = value);
    partial void OnWeightHistoryChanged(double value) => PushToOptions(o => o.WeightHistory = value);
    partial void OnWeightRiskChanged(double value) => PushToOptions(o => o.WeightRisk = value);
    partial void OnLockTolerancePercentChanged(double value) => PushToOptions(o => o.LockTolerancePercent = value);
    partial void OnLockWindowSecondsChanged(double value) => PushToOptions(o => o.LockWindowSeconds = value);
    partial void OnLockRequiredSecondsChanged(double value) => PushToOptions(o => o.LockRequiredSeconds = value);
    partial void OnKamikazeApproachAltitudeChanged(double value) => PushToOptions(o => o.KamikazeApproachAltitudeMeters = value);
    partial void OnKamikazeDiveAngleChanged(double value) => PushToOptions(o => o.KamikazeDiveAngleDegrees = value);
    partial void OnKamikazePullUpAltitudeChanged(double value) => PushToOptions(o => o.KamikazePullUpAltitudeMeters = value);
    partial void OnKamikazeMaxAttemptsChanged(int value) => PushToOptions(o => o.KamikazeMaxAttempts = value);
}
