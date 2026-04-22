using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

public partial class GeofenceSettingsViewModel : OptionsBackedViewModel<GeofenceOptions>
{
    public GeofenceSettingsViewModel() { }

    public GeofenceSettingsViewModel(GeofenceOptions options) : base(options)
    {
        _maxAltitudeMeters = options.MaxAltitudeMeters;
        _maxDistanceMeters = options.MaxDistanceMeters;
        _rtlAltitudeMeters = options.RtlAltitudeMeters;
        _bufferMeters = options.BufferMeters;
    }

    [ObservableProperty] private double _maxAltitudeMeters = 300;
    [ObservableProperty] private double _maxDistanceMeters = 2000;
    [ObservableProperty] private double _rtlAltitudeMeters = 120;
    [ObservableProperty] private double _bufferMeters = 100;

    partial void OnMaxAltitudeMetersChanged(double value) => PushToOptions(o => o.MaxAltitudeMeters = value);
    partial void OnMaxDistanceMetersChanged(double value) => PushToOptions(o => o.MaxDistanceMeters = value);
    partial void OnRtlAltitudeMetersChanged(double value) => PushToOptions(o => o.RtlAltitudeMeters = value);
    partial void OnBufferMetersChanged(double value) => PushToOptions(o => o.BufferMeters = value);
}
