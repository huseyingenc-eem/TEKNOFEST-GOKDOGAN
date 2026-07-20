using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.UI.Controls.Map.Layers;
using GOKDOGANIHA.UI.Controls.Map.Providers;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Map;

public partial class MapPanel : UserControl
{
    private readonly EnemyDroneLayer _enemyLayer;
    private readonly OwnshipLayer _ownshipLayer;
    private readonly QrTargetLayer _qrTargetLayer;
    private readonly HssZoneLayer _hssLayer;
    private readonly JammingZoneLayer _jammingLayer;
    private readonly WaypointLayer _waypointLayer;
    private readonly UserPolygonLayer _userPolygonLayer;
    private readonly TrailLayerRenderer _trailLayer;
    private readonly DispatcherTimer _trailFadeTimer;

    private MapViewModel? _viewModel;
    private MapOptions? _mapOptions;
    private GMapPolygon? _boundaryPolygon;

    public MapPanel()
    {
        InitializeComponent();

        _enemyLayer = new EnemyDroneLayer(MapCtrl);
        _ownshipLayer = new OwnshipLayer(MapCtrl);
        _qrTargetLayer = new QrTargetLayer(MapCtrl);
        _hssLayer = new HssZoneLayer(MapCtrl, this);
        _jammingLayer = new JammingZoneLayer(MapCtrl);
        _waypointLayer = new WaypointLayer(MapCtrl);
        _userPolygonLayer = new UserPolygonLayer(MapCtrl);
        _trailLayer = new TrailLayerRenderer(MapCtrl, this);

        _trailFadeTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _trailFadeTimer.Tick += OnTrailFadeTimerTick;

        ConfigureMap();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;

        MapCtrl.PreviewMouseLeftButtonDown += OnMapLeftButtonDown;
        MapCtrl.PreviewMouseRightButtonDown += OnMapRightButtonDown;
        MapCtrl.PreviewMouseMove += OnMapMouseMove;
    }

    private void ConfigureMap()
    {
        GMapProvider.UserAgent = "GOKDOGAN-YKI/1.0 (+TEKNOFEST-2026-SavasanIHA)";
        GMaps.Instance.Mode = AccessMode.ServerAndCache;
        MapCtrl.MapProvider = StadiaAlidadeSmoothDarkProvider.Instance;
        MapCtrl.Position = new PointLatLng(
            CompetitionBoundary.Center.Lat,
            CompetitionBoundary.Center.Lng);
        MapCtrl.MinZoom = 5;
        MapCtrl.MaxZoom = 19;
        MapCtrl.Zoom = 15;
        MapCtrl.DragButton = MouseButton.Left;
        MapCtrl.MouseWheelZoomEnabled = true;
        MapCtrl.ShowCenter = false;
        MapCtrl.CanDragMap = true;
        MapCtrl.OnMapZoomChanged += OnMapZoomChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _trailFadeTimer.Start();
        _mapOptions = App.AppOptions?.Map;
        if (_mapOptions is null)
        {
            DrawBoundary();
            return;
        }

        _mapOptions.PropertyChanged += OnMapOptionsChanged;
        ApplyAllMapOptions();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _trailFadeTimer.Stop();
        if (_mapOptions is not null)
            _mapOptions.PropertyChanged -= OnMapOptionsChanged;
    }

    private void OnTrailFadeTimerTick(object? sender, EventArgs e)
    {
        if (_viewModel is { ShowOwnTrail: true } && _trailLayer.HasRoutes)
            _trailLayer.Rebuild(_viewModel, DateTime.UtcNow);
    }

    private void OnMapOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MapOptions.TileProvider):
                ApplyTileProvider();
                break;
            case nameof(MapOptions.ShowBoundary):
                ApplyBoundary();
                break;
            case nameof(MapOptions.ShowHssZones):
                RebuildHss();
                break;
            case nameof(MapOptions.ShowGrid):
                ApplyGrid();
                break;
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
        if (_boundaryPolygon is not null) return;

        _boundaryPolygon = new GMapPolygon(CompetitionBoundary.Corners
            .Select(corner => new PointLatLng(corner.Lat, corner.Lng))
            .ToList())
        {
            Tag = "competition-boundary",
            ZIndex = 50
        };
        MapCtrl.Markers.Add(_boundaryPolygon);
        MapShapeStyler.Apply(
            _boundaryPolygon.Shape,
            new SolidColorBrush(Color.FromRgb(0x00, 0xD1, 0xC7)),
            thickness: 2.5,
            opacity: 0.9,
            fill: new SolidColorBrush(Color.FromArgb(0x12, 0x00, 0xD1, 0xC7)),
            dashed: true,
            dashArray: new DoubleCollection { 8, 4 });
    }

    private void RemoveBoundary()
    {
        if (_boundaryPolygon is null) return;
        MapCtrl.Markers.Remove(_boundaryPolygon);
        _boundaryPolygon = null;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UnsubscribeFromViewModel();
        _viewModel = DataContext as MapViewModel;
        if (_viewModel is null) return;

        _viewModel.EnemyDrones.CollectionChanged += OnEnemyDronesChanged;
        _viewModel.HssZones.CollectionChanged += OnHssZonesChanged;
        _viewModel.OwnTrail.CollectionChanged += OnTrailChanged;
        _viewModel.Waypoints.CollectionChanged += OnWaypointsChanged;
        _viewModel.JammingZones.CollectionChanged += OnJammingChanged;
        _viewModel.UserPolygon.CollectionChanged += OnUserPolygonChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        RebuildAllLayers();
        UpdateCursor();
    }

    private void UnsubscribeFromViewModel()
    {
        if (_viewModel is null) return;
        _viewModel.EnemyDrones.CollectionChanged -= OnEnemyDronesChanged;
        _viewModel.HssZones.CollectionChanged -= OnHssZonesChanged;
        _viewModel.OwnTrail.CollectionChanged -= OnTrailChanged;
        _viewModel.Waypoints.CollectionChanged -= OnWaypointsChanged;
        _viewModel.JammingZones.CollectionChanged -= OnJammingChanged;
        _viewModel.UserPolygon.CollectionChanged -= OnUserPolygonChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void RebuildAllLayers()
    {
        RebuildEnemies();
        RebuildHss();
        RebuildJamming();
        RebuildWaypoints();
        RebuildTrail();
        RebuildUserPolygon();
        UpdateOwnMarker();
        UpdateQrMarker();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
        if (_viewModel is not null) _enemyLayer.Rebuild(_viewModel.EnemyDrones);
    }

    private void RebuildHss()
    {
        if (_viewModel is not null)
            _hssLayer.Rebuild(_viewModel.HssZones, _mapOptions?.ShowHssZones != false);
    }

    private void RebuildJamming()
    {
        if (_viewModel is not null) _jammingLayer.Rebuild(_viewModel.JammingZones);
    }

    private void RebuildWaypoints()
    {
        if (_viewModel is not null) _waypointLayer.Rebuild(_viewModel.Waypoints);
    }

    private void RebuildTrail()
    {
        if (_viewModel is not null) _trailLayer.Rebuild(_viewModel, DateTime.UtcNow);
    }

    private void RebuildUserPolygon()
    {
        if (_viewModel is not null) _userPolygonLayer.Rebuild(_viewModel.UserPolygon);
    }

    private void UpdateOwnMarker()
    {
        if (_viewModel is not null) _ownshipLayer.Update(_viewModel);
    }

    private void UpdateQrMarker()
        => _qrTargetLayer.Update(_viewModel?.QrTarget);

    private void OnMapZoomChanged()
    {
        var zoom = (int)MapCtrl.Zoom;
        _hssLayer.UpdateZoom(zoom);
        _jammingLayer.UpdateZoom(zoom);
    }

    private void OnMapLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is not { IsDrawingPolygon: true }) return;
        var position = e.GetPosition(MapCtrl);
        var coordinate = MapCtrl.FromLocalToLatLng((int)position.X, (int)position.Y);
        _viewModel.AddPolygonVertex(coordinate.Lat, coordinate.Lng);
        e.Handled = true;
    }

    private void OnMapRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is not { IsDrawingPolygon: true }) return;
        _viewModel.CompletePolygonDraw();
        e.Handled = true;
    }

    private void OnMapMouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel is null) return;
        var position = e.GetPosition(MapCtrl);
        var coordinate = MapCtrl.FromLocalToLatLng((int)position.X, (int)position.Y);
        _viewModel.SetCursorPosition(coordinate.Lat, coordinate.Lng);
    }

    private void OnZoomInClick(object sender, RoutedEventArgs e)
        => MapCtrl.Zoom = Math.Min(MapCtrl.MaxZoom, MapCtrl.Zoom + 1);

    private void OnZoomOutClick(object sender, RoutedEventArgs e)
        => MapCtrl.Zoom = Math.Max(MapCtrl.MinZoom, MapCtrl.Zoom - 1);

    private void OnCenterOwnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.HasOwnPosition == true)
            MapCtrl.Position = new PointLatLng(
                _viewModel.OwnLatitude,
                _viewModel.OwnLongitude);
    }

    private void OnFitBoundaryClick(object sender, RoutedEventArgs e)
    {
        MapCtrl.Position = new PointLatLng(
            CompetitionBoundary.Center.Lat,
            CompetitionBoundary.Center.Lng);
        MapCtrl.Zoom = 14;
    }

    private void UpdateCursor()
    {
        if (_viewModel is not null)
            MapCtrl.Cursor = _viewModel.IsDrawingPolygon ? Cursors.Cross : Cursors.Arrow;
    }
}
