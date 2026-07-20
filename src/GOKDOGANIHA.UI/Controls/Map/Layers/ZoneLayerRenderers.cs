using System.Windows;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.UI.Controls.Map.Markers;

namespace GOKDOGANIHA.UI.Controls.Map.Layers;

internal sealed class HssZoneLayer
{
    private readonly GMapControl _map;
    private readonly FrameworkElement _resources;
    private readonly Dictionary<int, (GMapMarker Marker, HssCircleMarker Circle, HssKoordinat Data)> _markers = [];

    public HssZoneLayer(GMapControl map, FrameworkElement resources)
    {
        _map = map;
        _resources = resources;
    }

    public void Rebuild(IEnumerable<HssKoordinat> zones, bool isVisible)
    {
        Clear();
        if (!isVisible) return;

        var stroke = (Brush)_resources.FindResource("TacticalCritical");
        foreach (var zone in zones)
        {
            var circle = new HssCircleMarker(
                zone.Id,
                stroke,
                new SolidColorBrush(Color.FromArgb(40, 255, 82, 82)));
            circle.SetRadius(zone.YaricapMetre, zone.Enlem, (int)_map.Zoom);
            var marker = new GMapMarker(new PointLatLng(zone.Enlem, zone.Boylam))
            {
                Shape = circle,
                Offset = new Point(-circle.Width / 2, -circle.Height / 2)
            };
            _map.Markers.Add(marker);
            _markers[zone.Id] = (marker, circle, zone);
        }
    }

    public void UpdateZoom(int zoom)
    {
        foreach (var (marker, circle, data) in _markers.Values)
        {
            circle.SetRadius(data.YaricapMetre, data.Enlem, zoom);
            marker.Offset = new Point(-circle.Width / 2, -circle.Height / 2);
        }
    }

    private void Clear()
    {
        foreach (var (marker, _, _) in _markers.Values) _map.Markers.Remove(marker);
        _markers.Clear();
    }
}

internal sealed class JammingZoneLayer
{
    private readonly GMapControl _map;
    private readonly Dictionary<int, (GMapMarker Marker, JammingCircleMarker Circle, JammingZone Data)> _markers = [];

    public JammingZoneLayer(GMapControl map) => _map = map;

    public void Rebuild(IEnumerable<JammingZone> zones)
    {
        Clear();
        foreach (var zone in zones)
        {
            var circle = new JammingCircleMarker(zone.Id);
            circle.SetRadius(zone.RadiusMeters, zone.Latitude, (int)_map.Zoom);
            var marker = new GMapMarker(new PointLatLng(zone.Latitude, zone.Longitude))
            {
                Shape = circle,
                Offset = new Point(-circle.Width / 2, -circle.Height / 2)
            };
            _map.Markers.Add(marker);
            _markers[zone.Id] = (marker, circle, zone);
        }
    }

    public void UpdateZoom(int zoom)
    {
        foreach (var (marker, circle, data) in _markers.Values)
        {
            circle.SetRadius(data.RadiusMeters, data.Latitude, zoom);
            marker.Offset = new Point(-circle.Width / 2, -circle.Height / 2);
        }
    }

    private void Clear()
    {
        foreach (var (marker, _, _) in _markers.Values) _map.Markers.Remove(marker);
        _markers.Clear();
    }
}
