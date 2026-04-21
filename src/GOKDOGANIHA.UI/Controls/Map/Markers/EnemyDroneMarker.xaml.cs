using System.Windows;
using System.Windows.Controls;

namespace GOKDOGANIHA.UI.Controls.Map.Markers;

public partial class EnemyDroneMarker : UserControl
{
    public EnemyDroneMarker()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty HeadingAngleProperty =
        DependencyProperty.Register(nameof(HeadingAngle), typeof(double), typeof(EnemyDroneMarker),
            new PropertyMetadata(0.0, (d, e) =>
            {
                if (d is EnemyDroneMarker m) m.Heading.Angle = (double)e.NewValue;
            }));

    public double HeadingAngle
    {
        get => (double)GetValue(HeadingAngleProperty);
        set => SetValue(HeadingAngleProperty, value);
    }

    public static readonly DependencyProperty TeamNumberProperty =
        DependencyProperty.Register(nameof(TeamNumber), typeof(int), typeof(EnemyDroneMarker),
            new PropertyMetadata(0, (d, e) =>
            {
                if (d is EnemyDroneMarker m) m.TeamLabel.Text = "#" + ((int)e.NewValue).ToString();
            }));

    public int TeamNumber
    {
        get => (int)GetValue(TeamNumberProperty);
        set => SetValue(TeamNumberProperty, value);
    }
}
