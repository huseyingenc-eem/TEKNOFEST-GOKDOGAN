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
}
