using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GMap.NET;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.UI.ViewModels;

public sealed partial class MapViewModel : ObservableObject
{
    /// <summary>Kendi İHA iz çizgisinde tutulan max nokta sayısı (FIFO).</summary>
    private const int MaxTrailPoints = 200;

    private readonly TelemetryPollService? _telemetryPoll;
    private readonly HssPollService? _hssPoll;

    public ObservableCollection<KonumBilgisi> EnemyDrones { get; } = new();
    public ObservableCollection<HssKoordinat> HssZones { get; } = new();

    /// <summary>Kendi İHA'nın geçtiği yol — son MaxTrailPoints noktası.</summary>
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
    public MapViewModel() { }

    public MapViewModel(TelemetryPollService telemetryPoll, HssPollService hssPoll)
    {
        _telemetryPoll = telemetryPoll;
        _hssPoll = hssPoll;
        _telemetryPoll.TelemetryReceived += OnTelemetryReceived;
        _hssPoll.HssUpdated += OnHssUpdated;
    }

    public void SetOwnPosition(double lat, double lng, double headingDeg, bool isValid = true)
    {
        HasOwnPosition = isValid
            && lat is >= -90 and <= 90
            && lng is >= -180 and <= 180
            && (Math.Abs(lat) > 0.000001 || Math.Abs(lng) > 0.000001);
        if (!HasOwnPosition) return;

        OwnLatitude = lat;
        OwnLongitude = lng;
        OwnHeading = headingDeg;

        // İz çizgisi: yeni nokta yeterince uzaktıysa append (gürültüye karşı min mesafe).
        if (ShowOwnTrail) AppendTrailPoint(lat, lng);
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
        if (!isValid) HasOwnPosition = false;
        if (leavingSimulation)
        {
            // Simülasyondan canlıya geçildiğinde sahte HSS/rakip/iz verileri canlı
            // harita üzerinde kalmamalı. Gerçek poll cevapları koleksiyonları yeniden doldurur.
            HssZones.Clear();
            EnemyDrones.Clear();
            OwnTrail.Clear();
        }
    }

    public void SetCursorPosition(double lat, double lng)
    {
        CursorLatitude = lat;
        CursorLongitude = lng;
    }

    /// <summary>İz çizgisine yeni nokta ekler. Çok yakın noktaları ele eler.</summary>
    public void AppendTrailPoint(double lat, double lng)
    {
        if (OwnTrail.Count > 0)
        {
            var last = OwnTrail[OwnTrail.Count - 1];
            if (System.Math.Abs(last.Lat - lat) > 0.1 || System.Math.Abs(last.Lng - lng) > 0.1)
            {
                OwnTrail.Clear();
            }
            else if (System.Math.Abs(last.Lat - lat) < 0.00002 && System.Math.Abs(last.Lng - lng) < 0.00002)
            {
                return;
            }
        }
        OwnTrail.Add(new PointLatLng(lat, lng));
        while (OwnTrail.Count > MaxTrailPoints) OwnTrail.RemoveAt(0);
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
            foreach (var k in resp.KonumBilgileri
                         .Where(IsUsableOpponentPosition)
                         .GroupBy(x => x.TakimNumarasi)
                         .Select(group => group.OrderBy(x => x.ZamanFarkiMs).First())
                         .OrderBy(x => x.TakimNumarasi))
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

    private static bool IsUsableOpponentPosition(KonumBilgisi position)
        => position.TakimNumarasi > 0
           && double.IsFinite(position.Enlem)
           && double.IsFinite(position.Boylam)
           && position.Enlem is >= -90 and <= 90
           && position.Boylam is >= -180 and <= 180
           && position.ZamanFarkiMs is >= 0 and <= 5_000;

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
