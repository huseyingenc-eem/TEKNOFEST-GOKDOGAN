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
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.UI.Controls.Map.Markers;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Map;

public partial class MapPanel : UserControl
{
    private readonly Dictionary<int, GMapMarker> _enemyMarkers = new();
    private readonly Dictionary<int, (GMapMarker marker, HssCircleMarker circle, HssKoordinat data)> _hssMarkers = new();
    private GMapMarker? _ownMarker;
    private OwnDroneMarker? _ownVisual;
    private GMapMarker? _qrMarker;
    private GMapPolygon? _boundaryPoly;

    private MapViewModel? _vm;
    private MapOptions? _mapOptions;

    public MapPanel()
    {
        InitializeComponent();

        // Google tiles are used instead of OpenStreetMap: OSM volunteer-run
        // servers rate-limit/block heavy tactical use, and their tile policy
        // requires a specific UA + Referer combination that GMap.NET doesn't
        // expose cleanly. Google works without an API key via GMap.NET.
        GMapProvider.UserAgent = "GOKDOGAN-YKI/1.0 (+TEKNOFEST-2026-SavasanIHA)";

        GMaps.Instance.Mode = AccessMode.ServerAndCache;
        MapCtrl.MapProvider = GMapProviders.GoogleMap;
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
            case nameof(MapOptions.ShowGrid): /* reserved */ break;
        }
    }

    private void ApplyAllMapOptions()
    {
        ApplyTileProvider();
        ApplyBoundary();
        RebuildHss();
    }

    private void ApplyTileProvider()
    {
        if (_mapOptions is null) return;
        MapCtrl.MapProvider = _mapOptions.TileProvider switch
        {
            "OpenStreetMap" => GMapProviders.OpenStreetMap,
            "GoogleSatelliteMap" or "Google Satellite" => GMapProviders.GoogleSatelliteMap,
            "GoogleHybridMap" => GMapProviders.GoogleHybridMap,
            _ => GMapProviders.GoogleMap
        };
    }

    private void ApplyBoundary()
    {
        if (_mapOptions is null) return;
        if (_mapOptions.ShowBoundary) DrawBoundary();
        else RemoveBoundary();
    }

    private void DrawBoundary()
    {
        if (_boundaryPoly is not null) return; // already drawn
        var points = CompetitionBoundary.Corners
            .Select(c => new PointLatLng(c.Lat, c.Lng))
            .ToList();
        if (points.Count < 3) return;

        _boundaryPoly = new GMapPolygon(points) { Tag = "boundary" };
        MapCtrl.Markers.Add(_boundaryPoly);
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
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = DataContext as MapViewModel;
        if (_vm is null) return;

        _vm.EnemyDrones.CollectionChanged += OnEnemyDronesChanged;
        _vm.HssZones.CollectionChanged += OnHssZonesChanged;
        _vm.PropertyChanged += OnVmPropertyChanged;

        RebuildEnemies();
        RebuildHss();
        UpdateOwnMarker();
        UpdateQrMarker();
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MapViewModel.OwnLatitude):
            case nameof(MapViewModel.OwnLongitude):
            case nameof(MapViewModel.OwnHeading):
                UpdateOwnMarker();
                break;
            case nameof(MapViewModel.QrTarget):
                UpdateQrMarker();
                break;
        }
    }

    private void OnEnemyDronesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildEnemies();
    private void OnHssZonesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildHss();

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
    }

    private void UpdateOwnMarker()
    {
        if (_vm is null) return;
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
}
