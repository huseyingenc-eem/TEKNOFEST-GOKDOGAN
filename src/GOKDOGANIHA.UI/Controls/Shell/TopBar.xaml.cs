using System.Windows;
using System.Windows.Controls;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Shell;

public partial class TopBar : UserControl
{
    public TopBar() => InitializeComponent();

    // Fallback next to Command — guarantees the overlay opens even if the
    // Command binding fails to resolve (DataContext edge cases).
    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.IsSettingsOpen = true;
    }
}
