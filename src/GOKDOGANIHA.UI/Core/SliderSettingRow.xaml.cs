using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace GOKDOGANIHA.UI.Core;

/// <summary>
/// Standard settings row for a labelled numeric slider and its formatted value.
/// Keeping the layout and formatting here prevents every settings section from
/// rebuilding the same three-column XAML structure.
/// </summary>
public partial class SliderSettingRow : UserControl
{
    public SliderSettingRow()
    {
        InitializeComponent();
        UpdateFormattedValue();
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SliderSettingRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(SliderSettingRow),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnFormattingPropertyChanged));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(SliderSettingRow),
            new PropertyMetadata(0d));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(SliderSettingRow),
            new PropertyMetadata(100d));

    public static readonly DependencyProperty TickFrequencyProperty =
        DependencyProperty.Register(nameof(TickFrequency), typeof(double), typeof(SliderSettingRow),
            new PropertyMetadata(1d));

    public static readonly DependencyProperty IsSnapToTickEnabledProperty =
        DependencyProperty.Register(nameof(IsSnapToTickEnabled), typeof(bool), typeof(SliderSettingRow),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ValueFormatProperty =
        DependencyProperty.Register(nameof(ValueFormat), typeof(string), typeof(SliderSettingRow),
            new PropertyMetadata("0", OnFormattingPropertyChanged));

    public static readonly DependencyProperty PrefixProperty =
        DependencyProperty.Register(nameof(Prefix), typeof(string), typeof(SliderSettingRow),
            new PropertyMetadata(string.Empty, OnFormattingPropertyChanged));

    public static readonly DependencyProperty SuffixProperty =
        DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(SliderSettingRow),
            new PropertyMetadata(string.Empty, OnFormattingPropertyChanged));

    public static readonly DependencyProperty LabelMinWidthProperty =
        DependencyProperty.Register(nameof(LabelMinWidth), typeof(double), typeof(SliderSettingRow),
            new PropertyMetadata(120d));

    public static readonly DependencyProperty ValueMinWidthProperty =
        DependencyProperty.Register(nameof(ValueMinWidth), typeof(double), typeof(SliderSettingRow),
            new PropertyMetadata(60d));

    private static readonly DependencyPropertyKey FormattedValuePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(FormattedValue), typeof(string), typeof(SliderSettingRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty FormattedValueProperty =
        FormattedValuePropertyKey.DependencyProperty;

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double TickFrequency
    {
        get => (double)GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    public bool IsSnapToTickEnabled
    {
        get => (bool)GetValue(IsSnapToTickEnabledProperty);
        set => SetValue(IsSnapToTickEnabledProperty, value);
    }

    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    public string Prefix
    {
        get => (string)GetValue(PrefixProperty);
        set => SetValue(PrefixProperty, value);
    }

    public string Suffix
    {
        get => (string)GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public double LabelMinWidth
    {
        get => (double)GetValue(LabelMinWidthProperty);
        set => SetValue(LabelMinWidthProperty, value);
    }

    public double ValueMinWidth
    {
        get => (double)GetValue(ValueMinWidthProperty);
        set => SetValue(ValueMinWidthProperty, value);
    }

    public string FormattedValue => (string)GetValue(FormattedValueProperty);

    private static void OnFormattingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SliderSettingRow)d).UpdateFormattedValue();

    private void UpdateFormattedValue()
    {
        var formattedNumber = Value.ToString(ValueFormat, CultureInfo.CurrentCulture);
        SetValue(FormattedValuePropertyKey, $"{Prefix}{formattedNumber}{Suffix}");
    }
}
