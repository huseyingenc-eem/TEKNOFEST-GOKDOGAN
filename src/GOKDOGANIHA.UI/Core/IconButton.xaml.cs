using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Material.Icons;

namespace GOKDOGANIHA.UI.Core;

public enum IconButtonVariant
{
    Primary,
    Ok,
    Warn,
    Danger
}

public partial class IconButton : UserControl
{
    public IconButton() => InitializeComponent();

    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(MaterialIconKind), typeof(IconButton),
            new PropertyMetadata(MaterialIconKind.Circle));

    public MaterialIconKind IconKind
    {
        get => (MaterialIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public static readonly DependencyProperty IconBrushProperty =
        DependencyProperty.Register(nameof(IconBrush), typeof(Brush), typeof(IconButton),
            new PropertyMetadata(Brushes.White));

    public Brush IconBrush
    {
        get => (Brush)GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(IconButton),
            new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(IconButtonVariant), typeof(IconButton),
            new PropertyMetadata(IconButtonVariant.Primary));

    public IconButtonVariant Variant
    {
        get => (IconButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(IconButton),
            new PropertyMetadata(76.0));

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(IconButton),
            new PropertyMetadata(null));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(IconButton),
            new PropertyMetadata(null));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
