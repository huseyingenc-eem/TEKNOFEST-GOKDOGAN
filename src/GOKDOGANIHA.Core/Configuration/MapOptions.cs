using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Harita görünümü ve katman tercihleri. INotifyPropertyChanged — MapPanel
/// değişiklikleri canlı olarak uygular (tile provider swap, layer toggle).
/// </summary>
public sealed class MapOptions : INotifyPropertyChanged
{
    private string _tileProvider = "GoogleMap";
    private bool _showGrid = true;
    private bool _showBoundary = true;
    private bool _showHssZones = true;

    /// <summary>GMap.NET provider adı: "GoogleMap" / "OpenStreetMap" / "GoogleSatelliteMap".</summary>
    public string TileProvider { get => _tileProvider; set => Set(ref _tileProvider, value); }
    public bool ShowGrid { get => _showGrid; set => Set(ref _showGrid, value); }
    public bool ShowBoundary { get => _showBoundary; set => Set(ref _showBoundary, value); }
    public bool ShowHssZones { get => _showHssZones; set => Set(ref _showHssZones, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
