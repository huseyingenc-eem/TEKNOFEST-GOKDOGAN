using System.Windows;
using System.Windows.Controls;

namespace GOKDOGANIHA.UI.Controls.Map.Markers;

public partial class OwnDroneMarker : UserControl
{
    public OwnDroneMarker()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty HeadingAngleProperty =
        DependencyProperty.Register(nameof(HeadingAngle), typeof(double), typeof(OwnDroneMarker),
            new PropertyMetadata(0.0, (d, e) =>
            {
                if (d is OwnDroneMarker m) m.Heading.Angle = (double)e.NewValue;
            }));

    public double HeadingAngle
    {
        get => (double)GetValue(HeadingAngleProperty);
        set => SetValue(HeadingAngleProperty, value);
    }
}
