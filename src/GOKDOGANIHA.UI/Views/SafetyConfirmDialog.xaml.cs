using System.Windows;

namespace GOKDOGANIHA.UI.Views;

public partial class SafetyConfirmDialog : Window
{
    public SafetyConfirmDialog(string title, string message, string yesText, string noText)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        YesButton.Content = yesText;
        NoButton.Content = noText;
        Loaded += (_, _) => NoButton.Focus();
    }

    private void OnYesClick(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnNoClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
