using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GOKDOGANIHA.UI.Controls.Telemetry;

/// <summary>
/// İki state'li telemetri kartı: kompakt (HIZ/İRTİFA/BAT) ve genişletilmiş
/// (tüm metrikler + KONUM). Header tıklamasıyla toggle. Kompakt görünüm
/// pilot için anlık bakış, expanded analitik detay.
/// </summary>
public partial class TelemetryPanel : UserControl
{
    public TelemetryPanel()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TelemetryPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>True: tüm metrikler + KONUM. False: HIZ/İRTİFA/BAT kompakt kart.</summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private void OnHeaderClick(object sender, MouseButtonEventArgs e)
    {
        IsExpanded = !IsExpanded;
        e.Handled = true;
    }
}
