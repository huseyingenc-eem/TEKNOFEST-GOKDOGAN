using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GMap.NET;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Services.Telemetry;

namespace GOKDOGANIHA.UI.ViewModels;

public readonly record struct OwnTrailSample(
    PointLatLng Position,
    DateTime RecordedUtc,
    bool StartsNewSegment);

public sealed partial class MapViewModel : ObservableObject
{
    private const double MinTrailSpacingMeters = 3.0;
    private const double TrailBreakDistanceMeters = 2_000.0;
    private static readonly TimeSpan TrailBreakAfter = TimeSpan.FromSeconds(10);

    private readonly TelemetryPollService? _telemetryPoll;
    private readonly HssPollService? _hssPoll;
    private readonly OwnshipMapTrackFilter _ownshipTrackFilter = new();
    private readonly Dictionary<PointLatLng, TrailPointMetadata> _trailMetadata = new();

    public ObservableCollection<KonumBilgisi> EnemyDrones { get; } = new();
    public ObservableCollection<HssKoordinat> HssZones { get; } = new();

    /// <summary>Kendi İHA'nın geçtiği yol — kullanıcı temizleyene kadar korunur.</summary>
    public ObservableCollection<PointLatLng> OwnTrail { get; } = new();

    /// <summary>Mission planner waypoint'leri — Faz 4'te yalnızca görselleştirme.</summary>
    public ObservableCollection<Waypoint> Waypoints { get; } = new();

    /// <summary>Sinyal karıştırma bölgeleri — HSS'ten ayrı renk.</summary>
    public ObservableCollection<JammingZone> JammingZones { get; } = new();

    /// <summary>Kullanıcının çizdiği alan poligonu (yarışma yetkisinde olmayan
    /// özel bir bölge etiketi gibi). Faz 8'de Settings persistence ile diske yazılır.</summary>
    public ObservableCollection<PointLatLng> UserPolygon { get; } = new();

    [ObservableProperty] private QrKoordinat? qrTarget;
    [ObservableProperty] private double ownLatitude;
    [ObservableProperty] private double ownLongitude;
    [ObservableProperty] private double ownHeading;
    [ObservableProperty] private bool hasOwnPosition;
    [ObservableProperty] private string dataSourceLabel = "NONE";
    [ObservableProperty] private string vehicleLinkStatus = "Veri kaynağı bekleniyor";
    [ObservableProperty] private bool isSimulationMode;
    [ObservableProperty] private DateTime? lastVehicleUpdateUtc;
    [ObservableProperty] private double cursorLatitude;
    [ObservableProperty] private double cursorLongitude;
    [ObservableProperty] private bool followOwnship;

    /// <summary>Çizim modunda mı? True iken harita sol-tık vertex ekler, sağ-tık tamamlar.</summary>
    [ObservableProperty] private bool isDrawingPolygon;

    /// <summary>İz çizgisi gösterim toggle'ı — Settings ile persiste edilebilir.</summary>
    [ObservableProperty] private bool showOwnTrail = true;

    // Design-time / preview ctor (no services)
    public MapViewModel()
    {
        OwnTrail.CollectionChanged += OnOwnTrailCollectionChanged;
    }

    public MapViewModel(TelemetryPollService telemetryPoll, HssPollService hssPoll) : this()
    {
        _telemetryPoll = telemetryPoll;
        _hssPoll = hssPoll;
        _telemetryPoll.TelemetryReceived += OnTelemetryReceived;
        _hssPoll.HssUpdated += OnHssUpdated;
    }

    public void SetOwnPosition(
        double lat,
        double lng,
        double headingDeg,
        bool isValid = true,
        double? groundTrackDeg = null,
        double groundSpeedMps = 0,
        double? gpsHdop = null,
        DateTime? sampleUtc = null)
    {
        HasOwnPosition = isValid
            && lat is >= -90 and <= 90
            && lng is >= -180 and <= 180
            && (Math.Abs(lat) > 0.000001 || Math.Abs(lng) > 0.000001);
        if (!HasOwnPosition)
        {
            _ownshipTrackFilter.Reset();
            return;
        }

        var filtered = _ownshipTrackFilter.Apply(
            lat,
            lng,
            headingDeg,
            groundTrackDeg,
            groundSpeedMps,
            gpsHdop,
            sampleUtc ?? DateTime.UtcNow);
        if (!filtered.Accepted) return;

        OwnLatitude = filtered.Latitude;
        OwnLongitude = filtered.Longitude;
        OwnHeading = filtered.Heading;

        // İz çizgisi: yeni nokta yeterince uzaktıysa append (gürültüye karşı min mesafe).
        if (ShowOwnTrail) AppendTrailPoint(filtered.Latitude, filtered.Longitude);
    }

    public void SetVehicleStatus(
        bool isValid,
        string source,
        string status,
        bool simulation,
        DateTime? lastUpdateUtc)
    {
        var leavingSimulation = IsSimulationMode && !simulation;
        DataSourceLabel = source;
        VehicleLinkStatus = status;
        IsSimulationMode = simulation;
        LastVehicleUpdateUtc = lastUpdateUtc;
        if (!isValid)
        {
            HasOwnPosition = false;
            _ownshipTrackFilter.Reset();
        }
        if (leavingSimulation)
        {
            // Simülasyondan canlıya geçildiğinde sahte HSS/rakip/iz verileri canlı
            // harita üzerinde kalmamalı. Gerçek poll cevapları koleksiyonları yeniden doldurur.
            HssZones.Clear();
            EnemyDrones.Clear();
            OwnTrail.Clear();
            _ownshipTrackFilter.Reset();
        }
    }

    public void SetCursorPosition(double lat, double lng)
    {
        CursorLatitude = lat;
        CursorLongitude = lng;
    }

    /// <summary>
    /// İz çizgisine yeni nokta ekler. GPS titreşimini metre bazında eler ve
    /// kaynağın başka bir konuma sıçramasında eski iz ile sahte bağlantı kurmaz.
    /// </summary>
    public void AppendTrailPoint(double lat, double lng)
    {
        var nowUtc = DateTime.UtcNow;
        var startsNewSegment = OwnTrail.Count == 0;
        if (OwnTrail.Count > 0)
        {
            var last = OwnTrail[OwnTrail.Count - 1];
            var distanceMeters = DistanceMeters(last.Lat, last.Lng, lat, lng);
            var hasLongGap = _trailMetadata.TryGetValue(last, out var lastMetadata)
                             && nowUtc - lastMetadata.RecordedUtc > TrailBreakAfter;

            if (!hasLongGap && distanceMeters < MinTrailSpacingMeters)
            {
                return;
            }

            // Eski izi silmek yerine yeni ve bağımsız bir çizgi parçası başlat.
            startsNewSegment = hasLongGap || distanceMeters > TrailBreakDistanceMeters;
        }
        AddTrailPoint(lat, lng, nowUtc, startsNewSegment);
    }

    private void AddTrailPoint(
        double lat,
        double lng,
        DateTime recordedUtc,
        bool startsNewSegment)
    {
        var point = new PointLatLng(lat, lng);
        _trailMetadata[point] = new TrailPointMetadata(recordedUtc, startsNewSegment);
        OwnTrail.Add(point);
    }

    public IReadOnlyList<OwnTrailSample> GetOwnTrailSamples()
    {
        var nowUtc = DateTime.UtcNow;
        return OwnTrail
            .Select(point => new OwnTrailSample(
                point,
                _trailMetadata.TryGetValue(point, out var metadata)
                    ? metadata.RecordedUtc
                    : nowUtc,
                _trailMetadata.TryGetValue(point, out metadata)
                    ? metadata.StartsNewSegment
                    : OwnTrail.IndexOf(point) == 0))
            .ToList();
    }

    private void OnOwnTrailCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _trailMetadata.Clear();
            return;
        }

        if (e.OldItems is not null)
        {
            foreach (PointLatLng point in e.OldItems)
                _trailMetadata.Remove(point);
        }

        if (e.NewItems is not null)
        {
            var nowUtc = DateTime.UtcNow;
            foreach (PointLatLng point in e.NewItems)
            {
                var startsNewSegment = e.NewStartingIndex <= 0 && OwnTrail.IndexOf(point) == 0;
                _trailMetadata.TryAdd(point, new TrailPointMetadata(nowUtc, startsNewSegment));
            }
        }
    }

    private readonly record struct TrailPointMetadata(DateTime RecordedUtc, bool StartsNewSegment);

    private static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6_371_000.0;
        var lat1Rad = lat1 * Math.PI / 180.0;
        var lat2Rad = lat2 * Math.PI / 180.0;
        var deltaLat = (lat2 - lat1) * Math.PI / 180.0;
        var deltaLng = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(deltaLat / 2.0) * Math.Sin(deltaLat / 2.0)
                + Math.Cos(lat1Rad) * Math.Cos(lat2Rad)
                * Math.Sin(deltaLng / 2.0) * Math.Sin(deltaLng / 2.0);
        return earthRadiusMeters * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
    }

    public void ClearTrail() => OwnTrail.Clear();

    // ----- Waypoint helpers -----
    public void AddWaypoint(double lat, double lng, double alt, string action = "")
        => Waypoints.Add(new Waypoint(Waypoints.Count + 1, lat, lng, alt, action));

    public void ClearWaypoints() => Waypoints.Clear();

    // ----- Polygon draw helpers -----
    public void BeginPolygonDraw()
    {
        UserPolygon.Clear();
        IsDrawingPolygon = true;
    }

    public void AddPolygonVertex(double lat, double lng)
    {
        if (!IsDrawingPolygon) return;
        UserPolygon.Add(new PointLatLng(lat, lng));
    }

    public void CompletePolygonDraw() => IsDrawingPolygon = false;

    public void ClearPolygon()
    {
        UserPolygon.Clear();
        IsDrawingPolygon = false;
    }

    // ----- Toolbar commands -----
    [RelayCommand]
    private void ToggleDrawPolygon()
    {
        if (IsDrawingPolygon) CompletePolygonDraw();
        else BeginPolygonDraw();
    }

    [RelayCommand] private void ClearPolygonCmd() => ClearPolygon();
    [RelayCommand] private void ClearTrailCmd() => ClearTrail();
    [RelayCommand] private void ClearWaypointsCmd() => ClearWaypoints();
    [RelayCommand] private void ToggleTrail() => ShowOwnTrail = !ShowOwnTrail;

    /// <summary>
    /// TEST AMACIYLA SAHTE HSS VERİSİ ÜRETİCİSİ (Haritadaki kare/daire sorununu ve HSS'yi görmek için)
    /// </summary>
    [RelayCommand]
    private void GenerateMockHssCmd()
    {
        Marshal(() =>
        {
            if (App.AppOptions is not null)
                App.AppOptions.Map.ShowHssZones = true;

            HssZones.Clear();
            EnemyDrones.Clear();

            // Mock daireler her zaman yarışma merkezi (Ankara) etrafına düşsün.
            // Böylece kendi konumundan bağımsız olarak hep aynı yerde görünür.
            double baseLat = CompetitionBoundary.Center.Lat;
            double baseLng = CompetitionBoundary.Center.Lng;

            // Mock uçuş izi de iç dairede kalsın.
            OwnTrail.Clear();
            OwnTrail.Add(new PointLatLng(baseLat - 0.0025, baseLng - 0.0020));
            OwnTrail.Add(new PointLatLng(baseLat - 0.0010, baseLng + 0.0012));
            OwnTrail.Add(new PointLatLng(baseLat + 0.0018, baseLng + 0.0010));
            OwnTrail.Add(new PointLatLng(baseLat + 0.0022, baseLng - 0.0016));
            OwnTrail.Add(new PointLatLng(baseLat - 0.0006, baseLng - 0.0023));

            // 3 Adet Sahte HSS Dairesi (Ankara etrafında)
            HssZones.Add(new HssKoordinat(1, baseLat + 0.001, baseLng + 0.001, 50));
            HssZones.Add(new HssKoordinat(2, baseLat - 0.002, baseLng - 0.001, 80));
            HssZones.Add(new HssKoordinat(3, baseLat + 0.0015, baseLng - 0.002, 100));

            // Sahte Rakip İHA (Enemy Drone) (Haritada HSS ile birlikte İHA da görebilmek için)
            EnemyDrones.Add(new GOKDOGANIHA.Core.Models.Server.KonumBilgisi(
                TakimNumarasi: 99,
                Enlem: baseLat + 0.003,
                Boylam: baseLng - 0.001,
                Irtifa: 150,
                Dikilme: 0,
                Yonelme: 45,
                Yatis: 0,
                Hiz: 15,
                ZamanFarkiMs: 120
            ));
        });
    }

    private void OnTelemetryReceived(object? sender, TelemetryResponse resp)
    {
        Marshal(() =>
        {
            EnemyDrones.Clear();
            foreach (var k in OpponentTelemetrySanitizer.Clean(resp.KonumBilgileri))
            {
                EnemyDrones.Add(k);
            }
        });
    }

    private void OnHssUpdated(object? sender, HssResponse resp)
    {
        Marshal(() =>
        {
            HssZones.Clear();
            foreach (var h in resp.Koordinatlar.Where(IsUsableHssZone))
                HssZones.Add(h);
        });
    }

    private static bool IsUsableHssZone(HssKoordinat zone)
        => zone.Id >= 0
           && double.IsFinite(zone.Enlem)
           && double.IsFinite(zone.Boylam)
           && double.IsFinite(zone.YaricapMetre)
           && zone.Enlem is >= -90 and <= 90
           && zone.Boylam is >= -180 and <= 180
           && zone.YaricapMetre > 0;

    private static void Marshal(System.Action action)
    {
        var disp = Application.Current?.Dispatcher;
        if (disp is null || disp.CheckAccess()) action();
        else disp.Invoke(action);
    }
}
