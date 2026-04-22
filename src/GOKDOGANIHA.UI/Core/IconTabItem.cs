using System.Windows;
using System.Windows.Controls;
using Material.Icons;
using Material.Icons.WPF;

namespace GOKDOGANIHA.UI.Core;

/// <summary>
/// TabItem that auto-renders its header as "icon + label" from two simple properties.
/// Use inside any TabControl — the tactical style is picked up implicitly:
/// <code>
///   &lt;core:IconTabItem IconKind="Server" Label="SUNUCU"&gt; ... &lt;/core:IconTabItem&gt;
/// </code>
/// </summary>
public class IconTabItem : TabItem
{
    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(MaterialIconKind), typeof(IconTabItem),
            new PropertyMetadata(MaterialIconKind.Tab, OnHeaderPartChanged));

    public MaterialIconKind IconKind
    {
        get => (MaterialIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(IconTabItem),
            new PropertyMetadata(string.Empty, OnHeaderPartChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public IconTabItem()
    {
        BuildHeader();
    }

    private static void OnHeaderPartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconTabItem tab) tab.BuildHeader();
    }

    private void BuildHeader()
    {
        Header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                new MaterialIcon
                {
                    Kind = IconKind,
                    Width = 14,
                    Height = 14,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock
                {
                    Text = Label,
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }
}
