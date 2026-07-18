using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Telemetry;

namespace GOKDOGANIHA.Tests.Services;

public class FlightBackendCoordinatorTests
{
    [Fact]
    public async Task Switch_stops_previous_source_and_switches_command_target()
    {
        var state = new FlightState();
        var live = new FakeSource("LIVE");
        var simulation = new FakeSource("SIM");
        var liveCommands = new CaptureCommands();
        var simulationCommands = new CaptureCommands();
        var blocked = new CaptureCommands();
        var proxy = new SwitchableFlightCommandSink(blocked);
        using var coordinator = new FlightBackendCoordinator(
            state, live, simulation, liveCommands, simulationCommands, proxy, blocked);

        Assert.True((await coordinator.SwitchAsync(FlightDataMode.Live)).Success);
        proxy.Rtl();
        Assert.Equal(1, liveCommands.RtlCount);

        Assert.True((await coordinator.SwitchAsync(FlightDataMode.Simulation)).Success);
        proxy.Rtl();

        Assert.False(live.IsRunning);
        Assert.True(simulation.IsRunning);
        Assert.Equal(1, live.StopCount);
        Assert.Equal(1, simulationCommands.RtlCount);
        Assert.True(coordinator.IsSimulationActive);
    }

    [Fact]
    public async Task Failed_start_keeps_commands_blocked_and_state_unavailable()
    {
        var state = new FlightState();
        var live = new FakeSource("LIVE") { StartError = new InvalidOperationException("port busy") };
        var simulation = new FakeSource("SIM");
        var liveCommands = new CaptureCommands();
        var simulationCommands = new CaptureCommands();
        var blocked = new CaptureCommands();
        var proxy = new SwitchableFlightCommandSink(blocked);
        using var coordinator = new FlightBackendCoordinator(
            state, live, simulation, liveCommands, simulationCommands, proxy, blocked);

        var result = await coordinator.SwitchAsync(FlightDataMode.Live);
        proxy.Rtl();

        Assert.False(result.Success);
        Assert.Equal(FlightBackendStatus.Faulted, coordinator.Status);
        Assert.False(state.IsDataValid);
        Assert.Equal(1, blocked.RtlCount);
        Assert.Equal(0, liveCommands.RtlCount);
    }

    private sealed class FakeSource : IManagedFlightStateSource
    {
        public FakeSource(string name) => Name = name;
        public string Name { get; }
        public bool IsRunning { get; private set; }
        public bool IsReady => IsRunning;
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public Exception? StartError { get; init; }
        public event EventHandler<FlightSourceStatusChangedEventArgs>? StatusChanged;

        public void Start() => StartAsync().GetAwaiter().GetResult();

        public Task StartAsync(CancellationToken ct = default)
        {
            StartCount++;
            if (StartError is not null) throw StartError;
            IsRunning = true;
            StatusChanged?.Invoke(this,
                new FlightSourceStatusChangedEventArgs(FlightSourceStatus.Ready, $"{Name} ready"));
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopCount++;
            IsRunning = false;
            StatusChanged?.Invoke(this,
                new FlightSourceStatusChangedEventArgs(FlightSourceStatus.Stopped, $"{Name} stopped"));
            return Task.CompletedTask;
        }

        public void Dispose() => IsRunning = false;
    }

    private sealed class CaptureCommands : IFlightCommandSink
    {
        public int RtlCount { get; private set; }
        public void Arm() { }
        public void Disarm() { }
        public void SetMode(FlightMode mode) { }
        public void Rtl() => RtlCount++;
        public void Land() { }
        public void Loiter() { }
        public void GotoWaypoint(double latitude, double longitude, double altitudeMeters) { }
    }
}
