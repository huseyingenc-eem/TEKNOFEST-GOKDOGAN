using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using Material.Icons;

namespace GOKDOGANIHA.UI.Core;

public class OverlayHost : ContentControl
{
    private ButtonBase? _closeBtn;
    private Rectangle? _backdrop;
    private Window? _hookedWindow;

    static OverlayHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(OverlayHost),
            new FrameworkPropertyMetadata(typeof(OverlayHost)));

        // Start collapsed; OnIsOpenChanged flips Visibility imperatively.
        // Avoids Style.Triggers / Template.Triggers precedence quirks entirely.
        VisibilityProperty.OverrideMetadata(
            typeof(OverlayHost),
            new FrameworkPropertyMetadata(Visibility.Collapsed));
    }

    public OverlayHost()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(OverlayHost),
            new PropertyMetadata(false, OnIsOpenChanged));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(OverlayHost),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(OverlayHost),
            new PropertyMetadata(string.Empty));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly DependencyProperty HeaderIconKindProperty =
        DependencyProperty.Register(nameof(HeaderIconKind), typeof(MaterialIconKind), typeof(OverlayHost),
            new PropertyMetadata(MaterialIconKind.WindowMaximize));

    public MaterialIconKind HeaderIconKind
    {
        get => (MaterialIconKind)GetValue(HeaderIconKindProperty);
        set => SetValue(HeaderIconKindProperty, value);
    }

    public static readonly DependencyProperty ShowHeaderProperty =
        DependencyProperty.Register(nameof(ShowHeader), typeof(bool), typeof(OverlayHost),
            new PropertyMetadata(true));

    public bool ShowHeader
    {
        get => (bool)GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public static readonly DependencyProperty DismissOnBackdropClickProperty =
        DependencyProperty.Register(nameof(DismissOnBackdropClick), typeof(bool), typeof(OverlayHost),
            new PropertyMetadata(true));

    public bool DismissOnBackdropClick
    {
        get => (bool)GetValue(DismissOnBackdropClickProperty);
        set => SetValue(DismissOnBackdropClickProperty, value);
    }

    // Default leaves the TopBar (~48px + shadow) visible so global context stays readable.
    public static readonly DependencyProperty ContentMarginProperty =
        DependencyProperty.Register(nameof(ContentMargin), typeof(Thickness), typeof(OverlayHost),
            new PropertyMetadata(new Thickness(16, 72, 16, 16)));

    public Thickness ContentMargin
    {
        get => (Thickness)GetValue(ContentMarginProperty);
        set => SetValue(ContentMarginProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Unsubscribe previous template parts to prevent leaks on template re-apply (theme change etc.)
        if (_closeBtn != null) _closeBtn.Click -= OnCloseClicked;
        if (_backdrop != null) _backdrop.MouseLeftButtonDown -= OnBackdropClicked;

        _closeBtn = GetTemplateChild("PART_CloseButton") as ButtonBase;
        _backdrop = GetTemplateChild("PART_Backdrop") as Rectangle;

        if (_closeBtn != null) _closeBtn.Click += OnCloseClicked;
        if (_backdrop != null) _backdrop.MouseLeftButtonDown += OnBackdropClicked;
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

    private void OnBackdropClicked(object sender, MouseButtonEventArgs e)
    {
        if (DismissOnBackdropClick)
        {
            Close();
            e.Handled = true;
        }
    }

    private void Close() => IsOpen = false;

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not OverlayHost host) return;

        var isOpen = (bool)e.NewValue;
        // Set Visibility imperatively — no reliance on Style.Triggers precedence.
        host.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;

        if (isOpen)
        {
            // Defer focus — template may not be applied yet and element may not be visible.
            host.Dispatcher.BeginInvoke(new Action(() =>
            {
                host.Focus();
                Keyboard.Focus(host);
            }), DispatcherPriority.Input);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hookedWindow = Window.GetWindow(this);
        if (_hookedWindow != null)
            _hookedWindow.PreviewKeyDown += OnWindowPreviewKeyDown;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_hookedWindow != null)
        {
            _hookedWindow.PreviewKeyDown -= OnWindowPreviewKeyDown;
            _hookedWindow = null;
        }
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Respect handled state so stacked overlays don't all close on one Esc.
        if (e.Handled || !IsOpen || e.Key != Key.Escape) return;
        Close();
        e.Handled = true;
    }
}
