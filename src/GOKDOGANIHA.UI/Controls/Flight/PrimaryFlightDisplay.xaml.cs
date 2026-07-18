using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GOKDOGANIHA.UI.Controls.Flight;

/// <summary>
/// Primary Flight Display (PFD) — yapay ufuk + airspeed/altitude tape + heading compass.
/// Mockup'taki HUD overlay'in WPF port'u; okunaklı (400x250) tasarım, kamera fullscreen
/// overlay'inde sol-alt köşeye yerleşir.
///
/// SOLID: Pure DependencyProperty'ler ile bağımsız — herhangi bir VM property'si bağlanır.
/// Pitch/Roll/V/S görsel hesapları encapsulated callback'lerde — XAML converter karmaşası yok.
/// </summary>
public partial class PrimaryFlightDisplay : UserControl
{
    /// <summary>Pitch merdivenindeki 10° aralık 25 piksele karşılık gelir.</summary>
    private const double PitchPixelsPerDegree = 2.5;

    public PrimaryFlightDisplay() => InitializeComponent();

    public static readonly DependencyProperty IsDataValidProperty =
        DependencyProperty.Register(nameof(IsDataValid), typeof(bool), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(false));
    public bool IsDataValid { get => (bool)GetValue(IsDataValidProperty); set => SetValue(IsDataValidProperty, value); }

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
            new PropertyMetadata(0.0, OnHeadingChanged));
    public double Heading { get => (double)GetValue(HeadingProperty); set => SetValue(HeadingProperty, value); }

    public static readonly DependencyProperty AltitudeProperty =
        DependencyProperty.Register(nameof(Altitude), typeof(double), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(0.0, OnAltitudeChanged));
    public double Altitude { get => (double)GetValue(AltitudeProperty); set => SetValue(AltitudeProperty, value); }

    public static readonly DependencyProperty AirspeedProperty =
        DependencyProperty.Register(nameof(Airspeed), typeof(double), typeof(PrimaryFlightDisplay),
            new PropertyMetadata(0.0, OnAirspeedChanged));
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
        var pitch = FiniteOrZero((double)e.NewValue);
        self.PitchTransform.Y = Math.Clamp(pitch, -90, 90) * PitchPixelsPerDegree;
    }

    private static void OnRollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PrimaryFlightDisplay)d;
        // Sağ kanat aşağı = +Roll → horizon saat yönünün TERSİNE döner.
        self.RollTransform.Angle = -NormalizeSignedAngle(FiniteOrZero((double)e.NewValue));
    }

    private static void OnHeadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PrimaryFlightDisplay)d;
        var heading = NormalizeHeading(FiniteOrZero((double)e.NewValue));
        self.HeadingLeftText.Text = FormatHeading(heading - 10);
        self.HeadingText.Text = $"HDG {FormatHeading(heading)}°";
        self.HeadingRightText.Text = FormatHeading(heading + 10);
    }

    private static void OnAirspeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PrimaryFlightDisplay)d;
        var speed = Math.Max(0, FiniteOrZero((double)e.NewValue));
        self.SpeedUpperText.Text = FormatTapeValue(speed + 5);
        self.SpeedLowerText.Text = FormatTapeValue(Math.Max(0, speed - 5));
    }

    private static void OnAltitudeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PrimaryFlightDisplay)d;
        var altitude = FiniteOrZero((double)e.NewValue);
        self.AltitudeUpperText.Text = FormatTapeValue(altitude + 10);
        self.AltitudeLowerText.Text = FormatTapeValue(altitude - 10);
    }

    private static void OnVerticalSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PrimaryFlightDisplay)d;
        var v = FiniteOrZero((double)e.NewValue);
        // Düşüş hızlı (>3 m/s) → kırmızı; orta düşüş → sarı; yatay/tırmanış → yeşil.
        Brush brush = v < -3 ? (Brush)self.FindResource("TacticalCritical")
                    : v < 0  ? (Brush)self.FindResource("TacticalWarn")
                    :          (Brush)self.FindResource("TacticalOk");
        self.VerticalSpeedText.Foreground = brush;
    }

    private static double FiniteOrZero(double value) => double.IsFinite(value) ? value : 0;

    private static double NormalizeHeading(double heading)
    {
        var normalized = heading % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static double NormalizeSignedAngle(double angle)
    {
        var normalized = NormalizeHeading(angle);
        return normalized > 180 ? normalized - 360 : normalized;
    }

    private static string FormatHeading(double heading)
        => $"{(int)Math.Round(NormalizeHeading(heading)) % 360:000}";

    private static string FormatTapeValue(double value)
        => $"{(int)Math.Round(value):000}";
}
