using System.Windows;
using System.Windows.Controls;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    // PasswordBox.Password is not a DependencyProperty so it can't participate in normal binding.
    // Forward changes manually; VM owns the plaintext value.
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
            vm.Server.TeamPassword = pb.Password;
    }
}
