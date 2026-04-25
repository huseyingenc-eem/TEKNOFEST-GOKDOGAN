using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Material.Icons;

namespace GOKDOGANIHA.UI.Core;

/// <summary>
/// PanelFrame türevi. Header tıklanınca content collapse olur (▼/▶ chevron).
/// Sağ kolon (RightSidebar) için tasarlandı — mockup'taki accordion davranışı.
/// </summary>
public class CollapsiblePanelFrame : ContentControl
{
    static CollapsiblePanelFrame()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CollapsiblePanelFrame),
            new FrameworkPropertyMetadata(typeof(CollapsiblePanelFrame)));
    }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(CollapsiblePanelFrame),
            new PropertyMetadata(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(MaterialIconKind), typeof(CollapsiblePanelFrame),
            new PropertyMetadata(MaterialIconKind.Panorama));

    public MaterialIconKind IconKind
    {
        get => (MaterialIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(CollapsiblePanelFrame),
            new PropertyMetadata(true));

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>İsteğe bağlı renkli rozet (örn. "AKTİF" / "3" / "1.8 Hz").</summary>
    public static readonly DependencyProperty BadgeTextProperty =
        DependencyProperty.Register(nameof(BadgeText), typeof(string), typeof(CollapsiblePanelFrame),
            new PropertyMetadata(string.Empty));

    public string BadgeText
    {
        get => (string)GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Header") is FrameworkElement header)
        {
            header.MouseLeftButtonDown -= OnHeaderClick;
            header.MouseLeftButtonDown += OnHeaderClick;
        }
    }

    private void OnHeaderClick(object sender, MouseButtonEventArgs e)
    {
        IsExpanded = !IsExpanded;
        e.Handled = true;
    }
}
