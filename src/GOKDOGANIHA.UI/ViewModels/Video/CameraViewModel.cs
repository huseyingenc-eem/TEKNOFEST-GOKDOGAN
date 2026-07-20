using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GOKDOGANIHA.UI.ViewModels.Flight;

namespace GOKDOGANIHA.UI.ViewModels.Video;

public partial class CameraViewModel : ObservableObject
{
    private readonly FlightTelemetryViewModel _flight;

    public CameraViewModel(FlightTelemetryViewModel flight) => _flight = flight;

    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private string _recordingTime = "00:00";

    [RelayCommand]
    private void ZoomIn() => Zoom = Math.Min(20.0, Zoom + 0.5);

    [RelayCommand]
    private void ZoomOut() => Zoom = Math.Max(1.0, Zoom - 0.5);

    [RelayCommand]
    private void ToggleLock() => _flight.IsLocked = !_flight.IsLocked;

    [RelayCommand]
    private void ToggleBand()
    {
        // EO / IR selection will be delegated to the camera backend adapter.
    }

    [RelayCommand]
    private void Snapshot()
    {
        // Snapshot persistence will be delegated to the camera backend adapter.
    }
}
