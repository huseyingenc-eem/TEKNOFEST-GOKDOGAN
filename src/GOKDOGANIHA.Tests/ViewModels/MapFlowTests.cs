using GMap.NET;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.Tests.ViewModels;

public class MapFlowTests
{
    [Fact]
    public void Leaving_simulation_clears_simulation_only_map_state()
    {
        var vm = new MapViewModel();
        vm.SetVehicleStatus(true, "SIMULATION", "ready", true, DateTime.UtcNow);
        vm.OwnTrail.Add(new PointLatLng(41, 36));
        vm.EnemyDrones.Add(new KonumBilgisi(
            99, 41, 36, 100, 0, 0, 0, 10, 10));
        vm.HssZones.Add(new HssKoordinat(1, 41, 36, 50));

        vm.SetVehicleStatus(false, "MAVLINK", "waiting", false, null);

        Assert.Empty(vm.OwnTrail);
        Assert.Empty(vm.EnemyDrones);
        Assert.Empty(vm.HssZones);
        Assert.False(vm.HasOwnPosition);
    }

    [Fact]
    public void Invalid_or_zero_own_position_is_not_shown()
    {
        var vm = new MapViewModel();
        vm.SetOwnPosition(0, 0, 0, true);
        Assert.False(vm.HasOwnPosition);

        vm.SetOwnPosition(41.5, 36.1, 90, true);
        Assert.True(vm.HasOwnPosition);
    }

    [Fact]
    public void Own_trail_filters_sub_three_meter_gps_jitter()
    {
        var vm = new MapViewModel();

        vm.AppendTrailPoint(39.920000, 32.850000);
        vm.AppendTrailPoint(39.920005, 32.850000); // yaklaşık 0,56 m
        vm.AppendTrailPoint(39.920050, 32.850000); // yaklaşık 5,56 m

        Assert.Equal(2, vm.OwnTrail.Count);
    }

    [Fact]
    public void Own_trail_starts_a_new_segment_after_position_jump()
    {
        var vm = new MapViewModel();

        vm.AppendTrailPoint(39.9200, 32.8500);
        vm.AppendTrailPoint(39.9201, 32.8500);
        vm.AppendTrailPoint(39.9500, 32.8500); // 2 km'den fazla kaynak sıçraması

        Assert.Equal(3, vm.OwnTrail.Count);
        var samples = vm.GetOwnTrailSamples();
        Assert.False(samples[1].StartsNewSegment);
        Assert.True(samples[2].StartsNewSegment);
        Assert.Equal(39.9500, samples[2].Position.Lat, precision: 4);
    }

    [Fact]
    public void Own_marker_heading_prefers_mavlink_ground_track_over_noisy_position_bearing()
    {
        var vm = new MapViewModel();
        var start = new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc);
        vm.SetOwnPosition(39.9200, 32.8500, headingDeg: 15,
            groundTrackDeg: 90, groundSpeedMps: 25, sampleUtc: start);

        // Ham GPS noktası kuzeye kaysa bile hız vektörü doğuyu gösteriyor.
        vm.SetOwnPosition(39.9201, 32.8500, headingDeg: 15,
            groundTrackDeg: 90, groundSpeedMps: 25, sampleUtc: start.AddMilliseconds(500));
        Assert.InRange(vm.OwnHeading, 89.0, 91.0);
    }

    [Fact]
    public void Own_marker_circular_smoothing_crosses_north_without_turning_through_south()
    {
        var vm = new MapViewModel();
        var start = new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc);
        vm.SetOwnPosition(39.9200, 32.8500, 359,
            groundTrackDeg: 359, groundSpeedMps: 20, sampleUtc: start);

        vm.SetOwnPosition(39.92001, 32.8500, 1,
            groundTrackDeg: 1, groundSpeedMps: 20, sampleUtc: start.AddMilliseconds(100));

        Assert.True(vm.OwnHeading is >= 350 or <= 10);
    }

    [Fact]
    public void Implausible_short_interval_gps_spike_is_rejected()
    {
        var vm = new MapViewModel();
        var start = new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc);
        vm.SetOwnPosition(39.9200, 32.8500, 90,
            groundTrackDeg: 90, groundSpeedMps: 20, gpsHdop: 0.9, sampleUtc: start);
        vm.SetOwnPosition(39.9200, 32.8501, 90,
            groundTrackDeg: 90, groundSpeedMps: 20, gpsHdop: 0.9, sampleUtc: start.AddMilliseconds(500));
        var acceptedLatitude = vm.OwnLatitude;
        var acceptedLongitude = vm.OwnLongitude;

        vm.SetOwnPosition(40.0200, 32.8501, 270,
            groundTrackDeg: 270, groundSpeedMps: 20, gpsHdop: 0.9, sampleUtc: start.AddMilliseconds(600));

        Assert.Equal(acceptedLatitude, vm.OwnLatitude);
        Assert.Equal(acceptedLongitude, vm.OwnLongitude);
    }
}
