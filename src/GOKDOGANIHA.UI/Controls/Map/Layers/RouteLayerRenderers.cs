using System.Windows.Media;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.UI.Controls.Map.Markers;

namespace GOKDOGANIHA.UI.Controls.Map.Layers;

internal sealed class WaypointLayer
{
    private readonly GMapControl _map;
    private readonly List<GMapMarker> _markers = [];
    private GMapPolygon? _route;

    public WaypointLayer(GMapControl map) => _map = map;

    public void Rebuild(IEnumerable<Waypoint> waypoints)
    {
        Clear();
        var ordered = waypoints.OrderBy(waypoint => waypoint.Index).ToList();
        if (ordered.Count == 0) return;

        foreach (var waypoint in ordered)
        {
            var marker = new GMapMarker(new PointLatLng(waypoint.Latitude, waypoint.Longitude))
            {
                Shape = new WaypointMarker { WaypointIndex = waypoint.Index },
                Offset = new System.Windows.Point(-14, -14),
                ZIndex = 300
            };
            _map.Markers.Add(marker);
            _markers.Add(marker);
        }

        if (ordered.Count < 2) return;
        _route = new GMapPolygon(ordered
            .Select(waypoint => new PointLatLng(waypoint.Latitude, waypoint.Longitude))
            .ToList())
        {
            Tag = "route"
        };
        _map.Markers.Add(_route);
        MapShapeStyler.Apply(
            _route.Shape,
            new SolidColorBrush(Color.FromArgb(0xCC, 0x00, 0xD1, 0xC7)),
            thickness: 2,
            opacity: 1,
            fill: null,
            dashed: true,
            dashArray: new DoubleCollection { 6, 3 });
    }

    private void Clear()
    {
        foreach (var marker in _markers) _map.Markers.Remove(marker);
        _markers.Clear();
        if (_route is null) return;
        _map.Markers.Remove(_route);
        _route = null;
    }
}

internal sealed class UserPolygonLayer
{
    private readonly GMapControl _map;
    private GMapPolygon? _polygon;

    public UserPolygonLayer(GMapControl map) => _map = map;

    public void Rebuild(IEnumerable<PointLatLng> vertices)
    {
        if (_polygon is not null) _map.Markers.Remove(_polygon);
        _polygon = null;

        var points = vertices.ToList();
        if (points.Count < 2) return;

        _polygon = new GMapPolygon(points) { Tag = "user-polygon" };
        _map.Markers.Add(_polygon);
        MapShapeStyler.Apply(
            _polygon.Shape,
            new SolidColorBrush(Color.FromRgb(0xF1, 0xC4, 0x0F)),
            thickness: 2,
            opacity: 1,
            fill: new SolidColorBrush(Color.FromArgb(0x18, 0xF1, 0xC4, 0x0F)),
            dashed: true,
            dashArray: new DoubleCollection { 5, 3 });
    }
}
