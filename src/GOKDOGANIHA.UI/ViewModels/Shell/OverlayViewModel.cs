using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GOKDOGANIHA.UI.ViewModels.Shell;

public partial class OverlayViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyOpen))]
    private bool _isCameraFullscreen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyOpen))]
    private bool _isSettingsOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyOpen))]
    private bool _isKamikazeFullscreen;

    [ObservableProperty] private bool _isTelemetryExpanded;

    public bool IsAnyOpen => IsCameraFullscreen || IsSettingsOpen || IsKamikazeFullscreen;

    partial void OnIsCameraFullscreenChanged(bool value)
    {
        if (!value) return;
        IsSettingsOpen = false;
        IsKamikazeFullscreen = false;
    }

    partial void OnIsSettingsOpenChanged(bool value)
    {
        if (!value) return;
        IsCameraFullscreen = false;
        IsKamikazeFullscreen = false;
    }

    partial void OnIsKamikazeFullscreenChanged(bool value)
    {
        if (!value) return;
        IsCameraFullscreen = false;
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void ExpandCamera() => IsCameraFullscreen = true;

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void CloseActive()
    {
        IsCameraFullscreen = false;
        IsSettingsOpen = false;
    }
}
