using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// HARİTA sekmesi — tile sağlayıcı + katman görünürlükleri.
/// </summary>
public partial class MapSettingsViewModel : OptionsBackedViewModel<MapOptions>
{
    public MapSettingsViewModel() { }

    public MapSettingsViewModel(MapOptions options) : base(options)
    {
        _tileProvider = options.TileProvider;
        _showGrid = options.ShowGrid;
        _showBoundary = options.ShowBoundary;
        _showHssZones = options.ShowHssZones;
    }

    [ObservableProperty] private string _tileProvider = "GoogleMap";
    [ObservableProperty] private bool _showGrid = true;
    [ObservableProperty] private bool _showBoundary = true;
    [ObservableProperty] private bool _showHssZones = true;

    partial void OnTileProviderChanged(string value) => PushToOptions(o => o.TileProvider = value);
    partial void OnShowGridChanged(bool value) => PushToOptions(o => o.ShowGrid = value);
    partial void OnShowBoundaryChanged(bool value) => PushToOptions(o => o.ShowBoundary = value);
    partial void OnShowHssZonesChanged(bool value) => PushToOptions(o => o.ShowHssZones = value);
}
