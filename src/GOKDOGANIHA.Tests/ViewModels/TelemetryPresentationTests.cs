using GOKDOGANIHA.UI.ViewModels;
using GOKDOGANIHA.UI.ViewModels.Flight;

namespace GOKDOGANIHA.Tests.ViewModels;

public class TelemetryPresentationTests
{
    [Fact]
    public void Disconnected_vehicle_keeps_last_numeric_snapshot_with_short_status()
    {
        var vm = new FlightTelemetryViewModel(new MapViewModel())
        {
            FlightMode = "AUTO",
            Battery = 74,
            IsArmed = true,
            IsAutonomous = true,
            IsLocked = true,
            BackendStatusText = "BAĞLANTI KOPTU",
            HasVehicleTelemetrySnapshot = true,
            IsVehicleDataValid = false
        };

        Assert.False(vm.IsVehicleDataUnavailable);
        Assert.Equal("AUTO", vm.FlightModeDisplay);
        Assert.Equal("74%", vm.BatteryDisplay);
        Assert.Equal(74, vm.BatteryProgressValue);
        Assert.False(vm.IsArmedDisplay);
        Assert.False(vm.IsAutonomousDisplay);
        Assert.False(vm.IsLockedDisplay);
        Assert.Equal("BAĞLANTI KOPTU", vm.TelemetryAvailabilityMessage);
    }

    [Fact]
    public void Valid_vehicle_data_restores_all_shared_telemetry_presentations()
    {
        var vm = new FlightTelemetryViewModel(new MapViewModel())
        {
            FlightMode = "GUIDED",
            Battery = 68,
            IsArmed = true,
            IsAutonomous = true,
            IsLocked = true,
            HasVehicleTelemetrySnapshot = true,
            IsVehicleDataValid = true
        };

        Assert.False(vm.IsVehicleDataUnavailable);
        Assert.Equal("GUIDED", vm.FlightModeDisplay);
        Assert.Equal("68%", vm.BatteryDisplay);
        Assert.Equal(68, vm.BatteryProgressValue);
        Assert.True(vm.IsArmedDisplay);
        Assert.True(vm.IsAutonomousDisplay);
        Assert.True(vm.IsLockedDisplay);
        Assert.Equal(string.Empty, vm.TelemetryAvailabilityMessage);
    }

    [Fact]
    public void Before_first_packet_panel_reports_waiting_without_default_numbers()
    {
        var vm = new FlightTelemetryViewModel(new MapViewModel());

        Assert.True(vm.IsVehicleDataUnavailable);
        Assert.Equal("—", vm.FlightModeDisplay);
        Assert.Equal("—", vm.BatteryDisplay);
        Assert.Equal("TELEMETRİ BEKLENİYOR", vm.TelemetryAvailabilityMessage);
    }

    [Fact]
    public void Open_link_without_new_frames_reports_waiting_not_disconnected()
    {
        var vm = new FlightTelemetryViewModel(new MapViewModel())
        {
            HasVehicleTelemetrySnapshot = true,
            IsVehicleDataValid = false,
            BackendStatusText = "TELEMETRİ BEKLENİYOR"
        };

        Assert.Equal("TELEMETRİ BEKLENİYOR", vm.TelemetryAvailabilityMessage);
    }
}
