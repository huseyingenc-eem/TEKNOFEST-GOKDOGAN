using System.Windows;
using System.Windows.Controls;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Video;

public partial class VideoPanel : UserControl
{
    public VideoPanel() => InitializeComponent();

    // Fallback next to Command — guarantees the overlay opens even if the
    // Command binding fails to resolve (DataContext edge cases).
    private void OnExpandClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsCameraFullscreen = true;
    }
}
