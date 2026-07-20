using System.Windows;
using System.Windows.Controls;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Video;

public partial class VideoPanel : UserControl
{
    public VideoPanel() => InitializeComponent();

    private void OnExpandClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Overlay.IsCameraFullscreen = true;
    }
}
