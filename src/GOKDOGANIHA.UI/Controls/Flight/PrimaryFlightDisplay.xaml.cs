using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GOKDOGANIHA.UI.Controls.Flight;

/// <summary>
/// Primary Flight Display (PFD) — yapay ufuk + airspeed/altitude tape + heading compass.
/// Mockup'taki HUD overlay'in WPF port'u; kompakt (280x180) tasarım, kamera fullscreen
/// overlay'inde sol-alt köşeye yerleşir.
///
/// SOLID: Pure DependencyProperty'ler ile bağımsız — herhangi bir VM property'si bağlanır.
/// Pitch/Roll/V/S görsel hesapları encapsulated callback'lerde — XAML converter karmaşası yok.
/// </summary>
public partial class PrimaryFlightDisplay : UserControl
{
    /// <summary>1° pitch için kaç piksel Y öteleme — 90° = 162px (canvas yarısı kadar).</summary>
    private const double PitchPixelsPerDegree = 1.8;

    public PrimaryFlightDisplay() => InitializeComponent();

    public static readonly DependencyProperty PitchProperty =
        DependencyProperty.Register(nameof(Pitch), typeof(double), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(0.0, OnPitchChanged));
    public double Pitch { get => (double)GetValue(PitchProperty); set => SetValue(PitchProperty, value); }

    public static readonly DependencyProperty RollProperty =
        DependencyProperty.Register(nameof(Roll), typeof(double), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(0.0, OnRollChanged));
    public double Roll { get => (double)GetValue(RollProperty); set => SetValue(RollProperty, value); }

    public static readonly DependencyProperty HeadingProperty =
        DependencyProperty.Register(nameof(Heading), typeof(double), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(0.0));
    public double Heading { get => (double)GetValue(HeadingProperty); set => SetValue(HeadingProperty, value); }

    public static readonly DependencyProperty AltitudeProperty =
        DependencyProperty.Register(nameof(Altitude), typeof(double), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(0.0));
    public double Altitude { get => (double)GetValue(AltitudeProperty); set => SetValue(AltitudeProperty, value); }

    public static readonly DependencyProperty AirspeedProperty =
        DependencyProperty.Register(nameof(Airspeed), typeof(double), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(0.0));
    public double Airspeed { get => (double)GetValue(AirspeedProperty); set => SetValue(AirspeedProperty, value); }

    public static readonly DependencyProperty VerticalSpeedProperty =
        DependencyProperty.Register(nameof(VerticalSpeed), typeof(double), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(0.0, OnVerticalSpeedChanged));
    public double VerticalSpeed { get => (double)GetValue(VerticalSpeedProperty); set => SetValue(VerticalSpeedProperty, value); }

    // ----- Visual callbacks: XAML transform/style yerine code-behind (DRY + encapsulation) -----

    private static void OnPitchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PrimaryFlightDisplay)d;
        // 1° pitch yukarı = horizon line aşağı kaymalı (kullanıcı bakışı)
        self.PitchTransform.Y = (double)e.NewValue * PitchPixelsPerDegree;
    }

    private static void OnRollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PrimaryFlightDisplay)d;
        // Sağ kanat aşağı = +Roll → horizon saat yönünün TERSİNE döner.
        self.RollTransform.Angle = -(double)e.NewValue;
    }

    private static void OnVerticalSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PrimaryFlightDisplay)d;
        var v = (double)e.NewValue;
        // Düşüş hızlı (>3 m/s) → kırmızı; orta düşüş → sarı; yatay/tırmanış → yeşil.
        Brush brush = v < -3 ? (Brush)self.FindResource("TacticalCritical")
                    : v < 0  ? (Brush)self.FindResource("TacticalWarn")
                    :          (Brush)self.FindResource("TacticalOk");
        self.VerticalSpeedText.Foreground = brush;
    }
}
