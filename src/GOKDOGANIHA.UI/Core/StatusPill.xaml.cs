using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Material.Icons;

namespace GOKDOGANIHA.UI.Core;

public partial class StatusPill : UserControl
{
    public StatusPill() => InitializeComponent();

    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(MaterialIconKind), typeof(StatusPill),
            new PropertyMetadata(MaterialIconKind.Circle));

    public MaterialIconKind IconKind
    {
        get => (MaterialIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public static readonly DependencyProperty IconBrushProperty =
        DependencyProperty.Register(nameof(IconBrush), typeof(Brush), typeof(StatusPill),
            new PropertyMetadata(Brushes.White));

    public Brush IconBrush
    {
        get => (Brush)GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(StatusPill),
            new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatusPill),
            new PropertyMetadata(string.Empty));

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
