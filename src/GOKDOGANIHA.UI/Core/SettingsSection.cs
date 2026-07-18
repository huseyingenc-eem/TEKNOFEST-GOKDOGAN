using System.Windows;
using System.Windows.Controls;
using Material.Icons;

namespace GOKDOGANIHA.UI.Core;

/// <summary>
/// Settings sayfasındaki section'lar için reusable container.
/// Sol kolonda icon + başlık + subtitle, sağ kolonda Content (alanlar).
/// Style: Core/SettingsSection.xaml.
/// </summary>
public class SettingsSection : ContentControl
{
    static SettingsSection()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SettingsSection),
            new FrameworkPropertyMetadata(typeof(SettingsSection)));
    }

    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(MaterialIconKind), typeof(SettingsSection),
            new PropertyMetadata(MaterialIconKind.Cog));

    public MaterialIconKind IconKind
    {
        get => (MaterialIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsSection),
            new PropertyMetadata(""));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SettingsSection),
            new PropertyMetadata(""));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }
}
