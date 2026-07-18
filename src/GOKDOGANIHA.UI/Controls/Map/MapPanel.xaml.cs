using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.UI.Controls.Map.Markers;
using GOKDOGANIHA.UI.Controls.Map.Providers;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Map;

public partial class MapPanel : UserControl
{
    private readonly Dictionary<int, GMapMarker> _enemyMarkers = new();
    private readonly Dictionary<int, (GMapMarker marker, HssCircleMarker circle, HssKoordinat data)> _hssMarkers = new();
    private readonly Dictionary<int, (GMapMarker marker, JammingCircleMarker circle, JammingZone data)> _jammingMarkers = new();
    private readonly List<GMapMarker> _waypointMarkers = new();
    private GMapMarker? _ownMarker;
    private OwnDroneMarker? _ownVisual;
    private GMapMarker? _qrMarker;
    private GMapPolygon? _boundaryPoly;
    private GMapPolygon? _trailPoly;
    private GMapPolygon? _routePoly;
    private GMapPolygon? _userPoly;

    private MapViewModel? _vm;
    private MapOptions? _mapOptions;

    public MapPanel()
    {
        InitializeComponent();

        // Stadia Maps "Alidade Smooth Dark" — dark tactical basemap with
        // terrain/contour shading. Free tier allows localhost use without
        // an API key; for competition deployment, set
        // StadiaAlidadeSmoothDarkProvider.ApiKey at app startup.
        GMapProvider.UserAgent = "GOKDOGAN-YKI/1.0 (+TEKNOFEST-2026-SavasanIHA)";

        GMaps.Instance.Mode = AccessMode.ServerAndCache;
        MapCtrl.MapProvider = StadiaAlidadeSmoothDarkProvider.Instance;
        MapCtrl.Position = new PointLatLng(CompetitionBoundary.Center.Lat, CompetitionBoundary.Center.Lng);
        MapCtrl.MinZoom = 5;
        MapCtrl.MaxZoom = 19;
        MapCtrl.Zoom = 15;
        MapCtrl.DragButton = MouseButton.Left;
        MapCtrl.MouseWheelZoomEnabled = true;
        MapCtrl.ShowCenter = false;
        MapCtrl.CanDragMap = true;
        MapCtrl.OnMapZoomChanged += OnMapZoomChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;

        // Çizim modunda sol tık vertex ekler, sağ tık tamamlar.
        // Map'in drag davranışını çizim sırasında geçici olarak overide ediyoruz.
        MapCtrl.PreviewMouseLeftButtonDown += OnMapLeftButtonDown;
        MapCtrl.PreviewMouseRightButtonDown += OnMapRightButtonDown;
        MapCtrl.PreviewMouseMove += OnMapMouseMove;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // MapOptions (live INPC) — ayarlar değişince harita anında adapte olur.
        _mapOptions = App.AppOptions?.Map;
        if (_mapOptions is not null)
        {
            _mapOptions.PropertyChanged += OnMapOptionsChanged;
            ApplyAllMapOptions();
        }
        else
        {
            DrawBoundary();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_mapOptions is not null)
            _mapOptions.PropertyChanged -= OnMapOptionsChanged;
    }

    private void OnMapOptionsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MapOptions.TileProvider): ApplyTileProvider(); break;
            case nameof(MapOptions.ShowBoundary): ApplyBoundary(); break;
            case nameof(MapOptions.ShowHssZones): RebuildHss(); break;
            case nameof(MapOptions.ShowGrid): ApplyGrid(); break;
        }
    }

    private void ApplyAllMapOptions()
    {
        ApplyTileProvider();
        ApplyBoundary();
        RebuildHss();
        ApplyGrid();
    }

    private void ApplyTileProvider()
    {
        if (_mapOptions is null) return;
        MapCtrl.MapProvider = _mapOptions.TileProvider switch
        {
            "OpenStreetMap" => GMapProviders.OpenStreetMap,
            "GoogleMap" => GMapProviders.GoogleMap,
            "GoogleSatelliteMap" or "Google Satellite" => GMapProviders.GoogleSatelliteMap,
            "GoogleHybridMap" => GMapProviders.GoogleHybridMap,
            // Default: tactical dark basemap
            _ => StadiaAlidadeSmoothDarkProvider.Instance
        };
    }

    private void ApplyBoundary()
    {
        if (_mapOptions is null) return;
        if (_mapOptions.ShowBoundary) DrawBoundary();
        else RemoveBoundary();
    }

    private void ApplyGrid()
        => GridOverlay.Visibility = _mapOptions?.ShowGrid != false
            ? Visibility.Visible
            : Visibility.Collapsed;

    private void DrawBoundary()
    {
        // Güvenlik monitorü CompetitionBoundary.Corners poligonunu kullandığı için
        // harita da aynı geometriyi gösterir; görsel ve failsafe sınırı artık ayrışmaz.
        if (_boundaryPoly is not null) return;
        var points = CompetitionBoundary.Corners
            .Select(c => new PointLatLng(c.Lat, c.Lng))
            .ToList();
        _boundaryPoly = new GMapPolygon(points) { Tag = "competition-boundary", ZIndex = 50 };
        MapCtrl.Markers.Add(_boundaryPoly);
        ApplyShapeStyle(
            _boundaryPoly.Shape,
            new SolidColorBrush(Color.FromRgb(0x00, 0xD1, 0xC7)),
            thickness: 2.5,
            opacity: 0.9,
            fill: new SolidColorBrush(Color.FromArgb(0x12, 0x00, 0xD1, 0xC7)),
            dashed: true,
            dashArray: new DoubleCollection { 8, 4 });
    }

    private void RemoveBoundary()
    {
        if (_boundaryPoly is null) return;
        MapCtrl.Markers.Remove(_boundaryPoly);
        _boundaryPoly = null;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.EnemyDrones.CollectionChanged -= OnEnemyDronesChanged;
            _vm.HssZones.CollectionChanged -= OnHssZonesChanged;
            _vm.OwnTrail.CollectionChanged -= OnTrailChanged;
            _vm.Waypoints.CollectionChanged -= OnWaypointsChanged;
            _vm.JammingZones.CollectionChanged -= OnJammingChanged;
            _vm.UserPolygon.CollectionChanged -= OnUserPolygonChanged;
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = DataContext as MapViewModel;
        if (_vm is null) return;

        _vm.EnemyDrones.CollectionChanged += OnEnemyDronesChanged;
        _vm.HssZones.CollectionChanged += OnHssZonesChanged;
        _vm.OwnTrail.CollectionChanged += OnTrailChanged;
        _vm.Waypoints.CollectionChanged += OnWaypointsChanged;
        _vm.JammingZones.CollectionChanged += OnJammingChanged;
        _vm.UserPolygon.CollectionChanged += OnUserPolygonChanged;
        _vm.PropertyChanged += OnVmPropertyChanged;

        RebuildEnemies();
        RebuildHss();
        RebuildJamming();
        RebuildWaypoints();
        RebuildTrail();
        RebuildUserPolygon();
        UpdateOwnMarker();
        UpdateQrMarker();
        UpdateCursor();
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MapViewModel.OwnLatitude):
            case nameof(MapViewModel.OwnLongitude):
            case nameof(MapViewModel.OwnHeading):
            case nameof(MapViewModel.HasOwnPosition):
            case nameof(MapViewModel.FollowOwnship):
                UpdateOwnMarker();
                break;
            case nameof(MapViewModel.QrTarget):
                UpdateQrMarker();
                break;
            case nameof(MapViewModel.IsDrawingPolygon):
                UpdateCursor();
                break;
            case nameof(MapViewModel.ShowOwnTrail):
                RebuildTrail();
                break;
        }
    }

    private void OnEnemyDronesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildEnemies();
    private void OnHssZonesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildHss();
    private void OnJammingChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildJamming();
    private void OnWaypointsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildWaypoints();
    private void OnTrailChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildTrail();
    private void OnUserPolygonChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildUserPolygon();

    private void RebuildEnemies()
    {
        if (_vm is null) return;
        foreach (var marker in _enemyMarkers.Values) MapCtrl.Markers.Remove(marker);
        _enemyMarkers.Clear();

        foreach (var k in _vm.EnemyDrones)
        {
            var visual = new EnemyDroneMarker
            {
                TeamNumber = k.TakimNumarasi,
                HeadingAngle = k.Yonelme
            };
            var marker = new GMapMarker(new PointLatLng(k.Enlem, k.Boylam))
            {
                Shape = visual,
                Offset = new Point(-20, -27)
            };
            MapCtrl.Markers.Add(marker);
            _enemyMarkers[k.TakimNumarasi] = marker;
        }
    }

    private void RebuildHss()
    {
        if (_vm is null) return;
        foreach (var (marker, _, _) in _hssMarkers.Values) MapCtrl.Markers.Remove(marker);
        _hssMarkers.Clear();

        // ShowHssZones = false → çiz, boşaltıldıktan sonra geri çıkma
        if (_mapOptions is { ShowHssZones: false }) return;

        foreach (var h in _vm.HssZones)
        {
            var stroke = (Brush)FindResource("TacticalCritical");
            var fill = new SolidColorBrush(Color.FromArgb(40, 255, 82, 82));
            var circle = new HssCircleMarker(h.Id, stroke, fill);
            circle.SetRadius(h.YaricapMetre, h.Enlem, (int)MapCtrl.Zoom);

            var marker = new GMapMarker(new PointLatLng(h.Enlem, h.Boylam))
            {
                Shape = circle
            };
            marker.Offset = new Point(-circle.Width / 2, -circle.Height / 2);
            MapCtrl.Markers.Add(marker);
            _hssMarkers[h.Id] = (marker, circle, h);
        }
    }

    private void OnMapZoomChanged()
    {
        int z = (int)MapCtrl.Zoom;

        foreach (var (marker, circle, data) in _hssMarkers.Values)
        {
            circle.SetRadius(data.YaricapMetre, data.Enlem, z);
            marker.Offset = new Point(-circle.Width / 2, -circle.Height / 2);
        }
        foreach (var (marker, circle, data) in _jammingMarkers.Values)
        {
            circle.SetRadius(data.RadiusMeters, data.Latitude, z);
            marker.Offset = new Point(-circle.Width / 2, -circle.Height / 2);
        }
    }

    private void UpdateOwnMarker()
    {
        if (_vm is null) return;
        if (!_vm.HasOwnPosition)
        {
            if (_ownMarker is not null)
            {
                MapCtrl.Markers.Remove(_ownMarker);
                _ownMarker = null;
                _ownVisual = null;
            }
            return;
        }
        if (_ownMarker is null)
        {
            _ownVisual = new OwnDroneMarker();
            _ownMarker = new GMapMarker(new PointLatLng(_vm.OwnLatitude, _vm.OwnLongitude))
            {
                Shape = _ownVisual,
                Offset = new Point(-18, -18),
                ZIndex = 500
            };
            MapCtrl.Markers.Add(_ownMarker);
        }
        _ownMarker.Position = new PointLatLng(_vm.OwnLatitude, _vm.OwnLongitude);
        if (_ownVisual is not null) _ownVisual.HeadingAngle = _vm.OwnHeading;
        if (_vm.FollowOwnship) MapCtrl.Position = _ownMarker.Position;
    }

    private void UpdateQrMarker()
    {
        if (_vm is null) return;
        if (_vm.QrTarget is null)
        {
            if (_qrMarker is not null)
            {
                MapCtrl.Markers.Remove(_qrMarker);
                _qrMarker = null;
            }
            return;
        }

        _qrMarker ??= new GMapMarker(new PointLatLng(_vm.QrTarget.Enlem, _vm.QrTarget.Boylam))
        {
            Shape = new QrTargetMarker(),
            Offset = new Point(-17, -17),
            ZIndex = 400
        };
        _qrMarker.Position = new PointLatLng(_vm.QrTarget.Enlem, _vm.QrTarget.Boylam);
        if (!MapCtrl.Markers.Contains(_qrMarker)) MapCtrl.Markers.Add(_qrMarker);
    }

    // ============================== TRAIL ==============================
    private void RebuildTrail()
    {
        if (_trailPoly is not null)
        {
            MapCtrl.Markers.Remove(_trailPoly);
            _trailPoly = null;
        }
        if (_vm is null || !_vm.ShowOwnTrail || _vm.OwnTrail.Count < 2) return;

        // GMapPolygon polyline gibi de çalışır — Fill verilmediği sürece sadece stroke.
        _trailPoly = new GMapPolygon(_vm.OwnTrail.ToList()) { Tag = "trail" };
        MapCtrl.Markers.Add(_trailPoly);
        StyleTrailShape();
    }

    private void StyleTrailShape()
    {
        if (_trailPoly is null) return;
        var accent = (Brush)FindResource("TacticalAccent");
        // GMap.NET WPF GMapPolygon.Shape varsayılanda Path döndürür ama farklı
        // versiyonlarda Polygon olabilir — her iki yola da style uygula (DRY/safe).
        ApplyShapeStyle(_trailPoly.Shape, accent, thickness: 2.5, opacity: 0.55, fill: null, dashed: false);
    }

    private static void ApplyShapeStyle(System.Windows.UIElement? shape, Brush stroke, double thickness,
                                        double opacity, Brush? fill, bool dashed,
                                        DoubleCollection? dashArray = null)
    {
        if (shape is Path p)
        {
            p.Stroke = stroke; p.StrokeThickness = thickness;
            p.Opacity = opacity; p.Fill = fill;
            p.StrokeDashArray = dashed ? (dashArray ?? new DoubleCollection { 4, 3 }) : null!;
        }
        else if (shape is System.Windows.Shapes.Polygon poly)
        {
            poly.Stroke = stroke; poly.StrokeThickness = thickness;
            poly.Opacity = opacity; poly.Fill = fill;
            poly.StrokeDashArray = dashed ? (dashArray ?? new DoubleCollection { 4, 3 }) : null!;
        }
    }

    // ============================== WAYPOINTS ==============================
    private void RebuildWaypoints()
    {
        // Eski marker'ları kaldır
        foreach (var m in _waypointMarkers) MapCtrl.Markers.Remove(m);
        _waypointMarkers.Clear();
        if (_routePoly is not null) { MapCtrl.Markers.Remove(_routePoly); _routePoly = null; }

        if (_vm is null || _vm.Waypoints.Count == 0) return;

        // Pin'ler
        foreach (var w in _vm.Waypoints)
        {
            var pin = new WaypointMarker { WaypointIndex = w.Index };
            var marker = new GMapMarker(new PointLatLng(w.Latitude, w.Longitude))
            {
                Shape = pin,
                Offset = new Point(-14, -14),
                ZIndex = 300
            };
            MapCtrl.Markers.Add(marker);
            _waypointMarkers.Add(marker);
        }

        // Ardışık waypoint'leri birleştiren çizgi
        if (_vm.Waypoints.Count >= 2)
        {
            var points = _vm.Waypoints
                .OrderBy(w => w.Index)
                .Select(w => new PointLatLng(w.Latitude, w.Longitude))
                .ToList();
            _routePoly = new GMapPolygon(points) { Tag = "route" };
            MapCtrl.Markers.Add(_routePoly);
            ApplyShapeStyle(_routePoly.Shape,
                stroke: new SolidColorBrush(Color.FromArgb(0xCC, 0x00, 0xD1, 0xC7)),
                thickness: 2, opacity: 1.0, fill: null,
                dashed: true, dashArray: new DoubleCollection { 6, 3 });
        }
    }

    // ============================== JAMMING ZONES ==============================
    private void RebuildJamming()
    {
        foreach (var (m, _, _) in _jammingMarkers.Values) MapCtrl.Markers.Remove(m);
        _jammingMarkers.Clear();

        if (_vm is null) return;

        foreach (var z in _vm.JammingZones)
        {
            var circle = new JammingCircleMarker(z.Id);
            circle.SetRadius(z.RadiusMeters, z.Latitude, (int)MapCtrl.Zoom);
            var marker = new GMapMarker(new PointLatLng(z.Latitude, z.Longitude))
            {
                Shape = circle
            };
            marker.Offset = new Point(-circle.Width / 2, -circle.Height / 2);
            MapCtrl.Markers.Add(marker);
            _jammingMarkers[z.Id] = (marker, circle, z);
        }
    }

    // ============================== USER POLYGON (DRAW) ==============================
    private void RebuildUserPolygon()
    {
        if (_userPoly is not null)
        {
            MapCtrl.Markers.Remove(_userPoly);
            _userPoly = null;
        }
        if (_vm is null || _vm.UserPolygon.Count < 2) return;

        _userPoly = new GMapPolygon(_vm.UserPolygon.ToList()) { Tag = "user-polygon" };
        MapCtrl.Markers.Add(_userPoly);
        ApplyShapeStyle(_userPoly.Shape,
            stroke: new SolidColorBrush(Color.FromRgb(0xF1, 0xC4, 0x0F)),
            thickness: 2, opacity: 1.0,
            fill: new SolidColorBrush(Color.FromArgb(0x18, 0xF1, 0xC4, 0x0F)),
            dashed: true, dashArray: new DoubleCollection { 5, 3 });
    }

    // ============================== MOUSE HANDLERS ==============================
    private void OnMapLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm is null || !_vm.IsDrawingPolygon) return;

        // Çizim modunda map drag iptal — vertex ekleme öncelikli.
        var pos = e.GetPosition(MapCtrl);
        var latLng = MapCtrl.FromLocalToLatLng((int)pos.X, (int)pos.Y);
        _vm.AddPolygonVertex(latLng.Lat, latLng.Lng);
        e.Handled = true;
    }

    private void OnMapRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm is null || !_vm.IsDrawingPolygon) return;
        _vm.CompletePolygonDraw();
        e.Handled = true;
    }

    private void OnMapMouseMove(object sender, MouseEventArgs e)
    {
        if (_vm is null) return;
        var pos = e.GetPosition(MapCtrl);
        var latLng = MapCtrl.FromLocalToLatLng((int)pos.X, (int)pos.Y);
        _vm.SetCursorPosition(latLng.Lat, latLng.Lng);
    }

    private void OnZoomInClick(object sender, RoutedEventArgs e)
        => MapCtrl.Zoom = Math.Min(MapCtrl.MaxZoom, MapCtrl.Zoom + 1);

    private void OnZoomOutClick(object sender, RoutedEventArgs e)
        => MapCtrl.Zoom = Math.Max(MapCtrl.MinZoom, MapCtrl.Zoom - 1);

    private void OnCenterOwnClick(object sender, RoutedEventArgs e)
    {
        if (_vm?.HasOwnPosition == true)
            MapCtrl.Position = new PointLatLng(_vm.OwnLatitude, _vm.OwnLongitude);
    }

    private void OnFitBoundaryClick(object sender, RoutedEventArgs e)
    {
        MapCtrl.Position = new PointLatLng(CompetitionBoundary.Center.Lat, CompetitionBoundary.Center.Lng);
        MapCtrl.Zoom = 14;
    }

    private void UpdateCursor()
    {
        if (_vm is null) return;
        // Çizim modunda crosshair cursor — kullanıcıya görsel geri bildirim.
        MapCtrl.Cursor = _vm.IsDrawingPolygon ? Cursors.Cross : Cursors.Arrow;
    }
}
