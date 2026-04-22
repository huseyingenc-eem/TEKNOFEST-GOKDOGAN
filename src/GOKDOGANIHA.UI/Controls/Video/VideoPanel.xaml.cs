using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Video;

public partial class VideoPanel : UserControl
{
    private VideoOptions? _options;

    public VideoPanel()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            _options = App.AppOptions?.Video;
            if (_options is not null)
            {
                _options.PropertyChanged += OnOptionsChanged;
                ApplyStreamLabel();
            }
        };
        Unloaded += (_, _) =>
        {
            if (_options is not null) _options.PropertyChanged -= OnOptionsChanged;
        };
    }

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VideoOptions.RtspUrl)) ApplyStreamLabel();
    }

    private void ApplyStreamLabel()
    {
        if (_options is null) return;
        StreamLabel.Text = _options.RtspUrl;
    }

    private void OnExpandClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsCameraFullscreen = true;
    }
}
