using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Telemetry;
using GOKDOGANIHA.UI.ViewModels.Settings;

namespace GOKDOGANIHA.Tests.ViewModels;

public class TelemetrySettingsFlowTests
{
    [Fact]
    public async Task Simulation_does_not_start_when_modal_is_cancelled()
    {
        var fixture = new Fixture(confirm: false);

        await fixture.ViewModel.ToggleSimulationCommand.ExecuteAsync(null);

        Assert.Equal(0, fixture.Simulation.StartCount);
        Assert.Equal(1, fixture.Dialog.ConfirmCount);
        Assert.False(fixture.Coordinator.IsSimulationActive);
    }

    [Fact]
    public async Task Simulation_starts_only_after_modal_confirmation()
    {
        var fixture = new Fixture(confirm: true);

        await fixture.ViewModel.ToggleSimulationCommand.ExecuteAsync(null);

        Assert.Equal(1, fixture.Simulation.StartCount);
        Assert.Equal(1, fixture.Dialog.ConfirmCount);
        Assert.True(fixture.Coordinator.IsSimulationActive);
    }

    private sealed class Fixture : IDisposable
    {
        public Fixture(bool confirm)
        {
            Live = new FakeSource("LIVE");
            Simulation = new FakeSource("SIM");
            var blocked = new NullCommands();
            var proxy = new SwitchableFlightCommandSink(blocked);
            Coordinator = new FlightBackendCoordinator(
                new FlightState(),
                Live,
                Simulation,
                new NullCommands(),
                new NullCommands(),
                proxy,
                blocked);
            Dialog = new FakeDialog(confirm);
            ViewModel = new TelemetrySettingsViewModel(
                new TelemetryOptions(),
                new MavlinkOptions(),
                Coordinator,
                Dialog);
        }

        public FakeSource Live { get; }
        public FakeSource Simulation { get; }
        public FlightBackendCoordinator Coordinator { get; }
        public FakeDialog Dialog { get; }
        public TelemetrySettingsViewModel ViewModel { get; }
        public void Dispose() => Coordinator.Dispose();
    }

    private sealed class FakeDialog : IDialogService
    {
        private readonly bool _confirm;
        public FakeDialog(bool confirm) => _confirm = confirm;
        public int ConfirmCount { get; private set; }
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task ShowWarnAsync(string title, string message) => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
        public Task<bool> ConfirmAsync(
            string title,
            string message,
            string yesText = "Evet",
            string noText = "İptal")
        {
            ConfirmCount++;
            return Task.FromResult(_confirm);
        }
    }

    private sealed class FakeSource : IManagedFlightStateSource
    {
        public FakeSource(string name) => Name = name;
        public string Name { get; }
        public bool IsRunning { get; private set; }
        public bool IsReady => IsRunning;
        public int StartCount { get; private set; }
        public event EventHandler<FlightSourceStatusChangedEventArgs>? StatusChanged;
        public void Start() => StartAsync().GetAwaiter().GetResult();

        public Task StartAsync(CancellationToken ct = default)
        {
            StartCount++;
            IsRunning = true;
            StatusChanged?.Invoke(this,
                new FlightSourceStatusChangedEventArgs(FlightSourceStatus.Ready, $"{Name} ready"));
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            IsRunning = false;
            return Task.CompletedTask;
        }

        public void Dispose() => IsRunning = false;
    }

    private sealed class NullCommands : IFlightCommandSink
    {
        public void Arm() { }
        public void Disarm() { }
        public void SetMode(FlightMode mode) { }
        public void Rtl() { }
        public void Land() { }
        public void Loiter() { }
        public void GotoWaypoint(double latitude, double longitude, double altitudeMeters) { }
    }
}
