using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Material.Icons;

namespace GOKDOGANIHA.UI.Core;

[System.Windows.Markup.ContentProperty(nameof(Items))]
public partial class RadialMenu : UserControl
{
    public RadialMenu()
    {
        InitializeComponent();
        Items = new ObservableCollection<RadialMenuItem>();
        Loaded += (_, __) => Rebuild();
    }

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<RadialMenuItem>),
            typeof(RadialMenu), new PropertyMetadata(null, OnItemsPropertyChanged));

    public ObservableCollection<RadialMenuItem> Items
    {
        get => (ObservableCollection<RadialMenuItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private static void OnItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RadialMenu rm) return;
        if (e.OldValue is ObservableCollection<RadialMenuItem> oldCol)
            oldCol.CollectionChanged -= rm.OnItemsCollectionChanged;
        if (e.NewValue is ObservableCollection<RadialMenuItem> newCol)
            newCol.CollectionChanged += rm.OnItemsCollectionChanged;
        rm.Rebuild();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(RadialMenu),
            new PropertyMetadata(false, (d, _) => ((RadialMenu)d).OnIsOpenChanged()));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty RadiusProperty =
        DependencyProperty.Register(nameof(Radius), typeof(double), typeof(RadialMenu),
            new PropertyMetadata(110.0, (d, _) => ((RadialMenu)d).Rebuild()));

    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public static readonly DependencyProperty ItemSizeProperty =
        DependencyProperty.Register(nameof(ItemSize), typeof(double), typeof(RadialMenu),
            new PropertyMetadata(72.0, (d, _) => ((RadialMenu)d).Rebuild()));

    public double ItemSize
    {
        get => (double)GetValue(ItemSizeProperty);
        set => SetValue(ItemSizeProperty, value);
    }

    // Arc start angle in degrees. 0 = right, 90 = down, 180 = left, 270/-90 = up.
    // Default -90 (top) with 360 sweep == full circle starting at top.
    public static readonly DependencyProperty StartAngleProperty =
        DependencyProperty.Register(nameof(StartAngle), typeof(double), typeof(RadialMenu),
            new PropertyMetadata(-90.0, (d, _) => ((RadialMenu)d).Rebuild()));

    public double StartAngle
    {
        get => (double)GetValue(StartAngleProperty);
        set => SetValue(StartAngleProperty, value);
    }

    public static readonly DependencyProperty SweepAngleProperty =
        DependencyProperty.Register(nameof(SweepAngle), typeof(double), typeof(RadialMenu),
            new PropertyMetadata(360.0, (d, _) => ((RadialMenu)d).Rebuild()));

    public double SweepAngle
    {
        get => (double)GetValue(SweepAngleProperty);
        set => SetValue(SweepAngleProperty, value);
    }

    public static readonly DependencyProperty TriggerIconKindProperty =
        DependencyProperty.Register(nameof(TriggerIconKind), typeof(MaterialIconKind), typeof(RadialMenu),
            new PropertyMetadata(MaterialIconKind.DotsGrid, (d, _) => ((RadialMenu)d).UpdateTriggerIcon()));

    public MaterialIconKind TriggerIconKind
    {
        get => (MaterialIconKind)GetValue(TriggerIconKindProperty);
        set => SetValue(TriggerIconKindProperty, value);
    }

    public static readonly DependencyProperty TriggerIconWhenOpenKindProperty =
        DependencyProperty.Register(nameof(TriggerIconWhenOpenKind), typeof(MaterialIconKind), typeof(RadialMenu),
            new PropertyMetadata(MaterialIconKind.Close, (d, _) => ((RadialMenu)d).UpdateTriggerIcon()));

    public MaterialIconKind TriggerIconWhenOpenKind
    {
        get => (MaterialIconKind)GetValue(TriggerIconWhenOpenKindProperty);
        set => SetValue(TriggerIconWhenOpenKindProperty, value);
    }

    private void OnTriggerClick(object sender, RoutedEventArgs e) => IsOpen = !IsOpen;

    private void OnIsOpenChanged()
    {
        UpdateTriggerIcon();
        Animate(IsOpen);
    }

    private void UpdateTriggerIcon()
    {
        if (TriggerIcon == null) return;
        TriggerIcon.Kind = IsOpen ? TriggerIconWhenOpenKind : TriggerIconKind;

        var rot = (RotateTransform)TriggerIcon.RenderTransform;
        var rotAnim = new DoubleAnimation
        {
            To = IsOpen ? 45 : 0,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        rot.BeginAnimation(RotateTransform.AngleProperty, rotAnim);
    }

    private void Rebuild()
    {
        if (ItemsHost == null) return;

        double diameter = 2 * Radius + ItemSize + 16;
        RootGrid.Width = diameter;
        RootGrid.Height = diameter;

        ItemsHost.Children.Clear();
        if (Items == null || Items.Count == 0) return;

        double center = diameter / 2;
        int n = Items.Count;

        double startRad = StartAngle * Math.PI / 180.0;
        double sweepRad = SweepAngle * Math.PI / 180.0;
        bool fullCircle = Math.Abs(SweepAngle) >= 359.5;

        for (int i = 0; i < n; i++)
        {
            var data = Items[i];

            double t = n == 1 ? 0.5
                     : fullCircle ? (double)i / n
                     : (double)i / (n - 1);
            double angle = startRad + sweepRad * t;

            double x = center + Radius * Math.Cos(angle);
            double y = center + Radius * Math.Sin(angle);

            var btn = new IconButton
            {
                IconKind = data.IconKind,
                Label = data.Label ?? string.Empty,
                Variant = data.Variant,
                Size = ItemSize,
                Command = data.Command,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(IsOpen ? 1 : 0, IsOpen ? 1 : 0),
                Opacity = IsOpen ? 1 : 0,
                IsHitTestVisible = IsOpen
            };
            if (data.IconBrush != null) btn.IconBrush = data.IconBrush;

            Canvas.SetLeft(btn, x - ItemSize / 2);
            Canvas.SetTop(btn, y - ItemSize / 2);
            ItemsHost.Children.Add(btn);
        }
    }

    private void Animate(bool open)
    {
        if (ItemsHost == null) return;
        int n = ItemsHost.Children.Count;

        for (int i = 0; i < n; i++)
        {
            if (ItemsHost.Children[i] is not FrameworkElement btn) continue;

            double delay = open ? i * 35 : (n - 1 - i) * 25;
            var ease = new CubicEase { EasingMode = open ? EasingMode.EaseOut : EasingMode.EaseIn };

            var scaleAnim = new DoubleAnimation
            {
                To = open ? 1.0 : 0.0,
                Duration = TimeSpan.FromMilliseconds(open ? 240 : 160),
                BeginTime = TimeSpan.FromMilliseconds(delay),
                EasingFunction = ease
            };
            var opacityAnim = new DoubleAnimation
            {
                To = open ? 1.0 : 0.0,
                Duration = TimeSpan.FromMilliseconds(open ? 200 : 140),
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };

            if (btn.RenderTransform is ScaleTransform st)
            {
                st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
            btn.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            btn.IsHitTestVisible = open;
        }
    }
}
