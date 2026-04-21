using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.UI.ViewModels;

public sealed partial class MapViewModel : ObservableObject
{
    private readonly TelemetryPollService? _telemetryPoll;
    private readonly HssPollService? _hssPoll;

    public ObservableCollection<KonumBilgisi> EnemyDrones { get; } = new();
    public ObservableCollection<HssKoordinat> HssZones { get; } = new();

    [ObservableProperty] private QrKoordinat? qrTarget;
    [ObservableProperty] private double ownLatitude;
    [ObservableProperty] private double ownLongitude;
    [ObservableProperty] private double ownHeading;

    // Design-time / preview ctor (no services)
    public MapViewModel() { }

    public MapViewModel(TelemetryPollService telemetryPoll, HssPollService hssPoll)
    {
        _telemetryPoll = telemetryPoll;
        _hssPoll = hssPoll;
        _telemetryPoll.TelemetryReceived += OnTelemetryReceived;
        _hssPoll.HssUpdated += OnHssUpdated;
    }

    public void SetOwnPosition(double lat, double lng, double headingDeg)
    {
        OwnLatitude = lat;
        OwnLongitude = lng;
        OwnHeading = headingDeg;
    }

    private void OnTelemetryReceived(object? sender, TelemetryResponse resp)
    {
        Marshal(() =>
        {
            EnemyDrones.Clear();
            foreach (var k in resp.KonumBilgileri) EnemyDrones.Add(k);
        });
    }

    private void OnHssUpdated(object? sender, HssResponse resp)
    {
        Marshal(() =>
        {
            HssZones.Clear();
            foreach (var h in resp.Koordinatlar) HssZones.Add(h);
        });
    }

    private static void Marshal(Action action)
    {
        var disp = Application.Current?.Dispatcher;
        if (disp is null || disp.CheckAccess()) action();
        else disp.Invoke(action);
    }
}
