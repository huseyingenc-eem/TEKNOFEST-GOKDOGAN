using System.Windows;
using System.Windows.Controls;
using Material.Icons;

namespace GOKDOGANIHA.UI.Core;

public class PanelFrame : ContentControl
{
    static PanelFrame()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PanelFrame),
            new FrameworkPropertyMetadata(typeof(PanelFrame)));
    }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(PanelFrame),
            new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(MaterialIconKind), typeof(PanelFrame),
            new PropertyMetadata(MaterialIconKind.Panorama));

    public MaterialIconKind IconKind
    {
        get => (MaterialIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }
}
