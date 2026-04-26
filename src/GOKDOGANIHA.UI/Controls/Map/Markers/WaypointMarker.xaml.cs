using System.Windows;
using System.Windows.Controls;

namespace GOKDOGANIHA.UI.Controls.Map.Markers;

public partial class WaypointMarker : UserControl
{
    public WaypointMarker() => InitializeComponent();

    public static readonly DependencyProperty WaypointIndexProperty =
        DependencyProperty.Register(nameof(WaypointIndex), typeof(int), typeof(WaypointMarker),
            new PropertyMetadata(0));

    public int WaypointIndex
    {
        get => (int)GetValue(WaypointIndexProperty);
        set => SetValue(WaypointIndexProperty, value);
    }
}
