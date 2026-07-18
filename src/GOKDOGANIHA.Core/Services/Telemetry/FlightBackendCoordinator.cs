using System.ComponentModel;
using System.Runtime.CompilerServices;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Core.Services.Telemetry;

/// <summary>
/// Canlı ve simülasyon uçuş backend'lerini tek yazarlı ve atomik biçimde değiştirir.
/// Veri kaynağı ile komut hedefi her zaman birlikte değiştirilir.
/// </summary>
public sealed class FlightBackendCoordinator : INotifyPropertyChanged, IDisposable
{
    private readonly FlightState _state;
    private readonly IManagedFlightStateSource _liveSource;
    private readonly IManagedFlightStateSource _simulationSource;
    private readonly IFlightCommandSink _liveCommands;
    private readonly IFlightCommandSink _simulationCommands;
    private readonly IFlightCommandSink _transitionBlockedCommands;
    private readonly SwitchableFlightCommandSink _commands;
    private readonly SemaphoreSlim _switchGate = new(1, 1);

    private IManagedFlightStateSource? _activeSource;
    private FlightDataMode _activeMode = FlightDataMode.Live;
    private FlightBackendStatus _status = FlightBackendStatus.Disconnected;
    private string _statusMessage = "Uçuş veri kaynağı başlatılmadı";
    private bool _disposed;

    public FlightBackendCoordinator(
        FlightState state,
        IManagedFlightStateSource liveSource,
        IManagedFlightStateSource simulationSource,
        IFlightCommandSink liveCommands,
        IFlightCommandSink simulationCommands,
        SwitchableFlightCommandSink commands,
        IFlightCommandSink transitionBlockedCommands)
    {
        _state = state;
        _liveSource = liveSource;
        _simulationSource = simulationSource;
        _liveCommands = liveCommands;
        _simulationCommands = simulationCommands;
        _commands = commands;
        _transitionBlockedCommands = transitionBlockedCommands;
        _liveSource.StatusChanged += OnSourceStatusChanged;
        _simulationSource.StatusChanged += OnSourceStatusChanged;
    }

    public FlightDataMode ActiveMode
    {
        get => _activeMode;
        private set { if (_activeMode != value) { _activeMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSimulationActive)); } }
    }

    public FlightBackendStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLiveReady));
            OnPropertyChanged(nameof(IsSimulationActive));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public bool IsSimulationActive => ActiveMode == FlightDataMode.Simulation
        && Status == FlightBackendStatus.Simulation;

    public bool IsLiveReady => ActiveMode == FlightDataMode.Live
        && Status == FlightBackendStatus.Live;

    public async Task<FlightModeSwitchResult> SwitchAsync(
        FlightDataMode requestedMode,
        CancellationToken ct = default,
        bool forceRestart = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _switchGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!forceRestart && _activeSource is not null && ActiveMode == requestedMode && _activeSource.IsRunning)
            {
                return new FlightModeSwitchResult(true, requestedMode, StatusMessage);
            }

            Status = FlightBackendStatus.Switching;
            StatusMessage = "Aktif uçuş kaynağı güvenli biçimde durduruluyor";
            _commands.SetTarget(_transitionBlockedCommands);

            if (_activeSource is not null)
            {
                try
                {
                    await _activeSource.StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Status = FlightBackendStatus.Faulted;
                    StatusMessage = $"{_activeSource.Name} durdurulamadı: {ex.Message}";
                    _state.MarkUnavailable(_activeSource.Name);
                    return new FlightModeSwitchResult(false, requestedMode, StatusMessage);
                }
            }

            ActiveMode = requestedMode;
            _state.MarkUnavailable(requestedMode == FlightDataMode.Live ? "MAVLINK" : "SIMULATION");

            _activeSource = requestedMode == FlightDataMode.Live ? _liveSource : _simulationSource;
            Status = requestedMode == FlightDataMode.Live
                ? FlightBackendStatus.ConnectingLive
                : FlightBackendStatus.StartingSimulation;
            StatusMessage = requestedMode == FlightDataMode.Live
                ? "MAVLink UDP dinleyici başlatılıyor"
                : "Simülasyon senaryosu başlatılıyor";

            try
            {
                await _activeSource.StartAsync(ct).ConfigureAwait(false);
                _commands.SetTarget(requestedMode == FlightDataMode.Live ? _liveCommands : _simulationCommands);
                ApplySourceStatus(_activeSource,
                    _activeSource.IsReady ? FlightSourceStatus.Ready : FlightSourceStatus.WaitingForData,
                    _activeSource.IsReady ? $"{_activeSource.Name} hazır" : $"{_activeSource.Name} veri bekliyor");
                return new FlightModeSwitchResult(true, requestedMode, StatusMessage);
            }
            catch (Exception ex)
            {
                try { await _activeSource.StopAsync().ConfigureAwait(false); } catch { }
                _commands.SetTarget(_transitionBlockedCommands);
                _state.MarkUnavailable(_activeSource.Name);
                Status = FlightBackendStatus.Faulted;
                StatusMessage = $"{_activeSource.Name} başlatılamadı: {ex.Message}";
                return new FlightModeSwitchResult(false, requestedMode, StatusMessage);
            }
        }
        finally
        {
            _switchGate.Release();
        }
    }

    private void OnSourceStatusChanged(object? sender, FlightSourceStatusChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, _activeSource) || sender is not IManagedFlightStateSource source) return;
        ApplySourceStatus(source, e.Status, e.Message);
    }

    private void ApplySourceStatus(IManagedFlightStateSource source, FlightSourceStatus sourceStatus, string message)
    {
        if (!ReferenceEquals(source, _activeSource)) return;

        Status = sourceStatus switch
        {
            FlightSourceStatus.Ready when ActiveMode == FlightDataMode.Live => FlightBackendStatus.Live,
            FlightSourceStatus.Ready => FlightBackendStatus.Simulation,
            FlightSourceStatus.Starting when ActiveMode == FlightDataMode.Live => FlightBackendStatus.ConnectingLive,
            FlightSourceStatus.WaitingForData when ActiveMode == FlightDataMode.Live => FlightBackendStatus.ConnectingLive,
            FlightSourceStatus.Starting => FlightBackendStatus.StartingSimulation,
            FlightSourceStatus.WaitingForData => FlightBackendStatus.StartingSimulation,
            FlightSourceStatus.Faulted => FlightBackendStatus.Faulted,
            _ => FlightBackendStatus.Disconnected
        };
        StatusMessage = message;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _liveSource.StatusChanged -= OnSourceStatusChanged;
        _simulationSource.StatusChanged -= OnSourceStatusChanged;
        _commands.SetTarget(_transitionBlockedCommands);
        try { _liveSource.Dispose(); } catch { }
        if (!ReferenceEquals(_simulationSource, _liveSource))
        {
            try { _simulationSource.Dispose(); } catch { }
        }
        _switchGate.Dispose();
    }
}
