using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GOKDOGANIHA.UI.ViewModels;
using GOKDOGANIHA.UI.ViewModels.Flight;

namespace GOKDOGANIHA.UI.Controls.Kamikaze;

/// <summary>
/// Kamikaze görevinin terminal fazı için tam-ekran taktik görünüm. Mockup'taki
/// APPROACH GEOMETRY (sol, SVG path tabanlı dinamik dalış grafiği) +
/// OPTICAL TARGETING (sağ, kamera + QR detection) + ABORT layout.
///
/// SVG Path görselleştirmesi: code-behind, Flight alt ViewModel'inin Altitude/Pitch
/// özelliklerini izleyip drone ikonu Y konumunu ve bezier eğrisinin başlangıç +
/// kontrol noktalarını canlı set eder. Pilot, sayılar yerine **görsel olarak**
/// uçağın hedefe yaklaşımını değerlendirir (modern PFD prensibi).
/// </summary>
public partial class KamikazeFullscreenView : UserControl
{
    /// <summary>Yaklaşma yörüngesinin yer hattındaki bitiş x-koordinatı (Canvas 500x340 native).</summary>
    private const double TargetX = 411;
    private const double GroundY = 276;
    /// <summary>Drone başlangıç x-koordinatı (sabit — sol üstten dalmaya başlar).</summary>
    private const double DroneStartX = 80;
    /// <summary>Yaklaşma irtifası 100 m → Canvas Y=80; pull-up irtifası 30 m → Y=240. 1m ≈ 2.28 px.</summary>
    private const double AltitudeReference = 100;
    private const double DroneTopY = 80;
    private const double PixelsPerMeter = (GroundY - DroneTopY) / AltitudeReference;

    private FlightTelemetryViewModel? _flight;
    private MapViewModel? _map;

    public KamikazeFullscreenView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_flight is not null) _flight.PropertyChanged -= OnFlightPropertyChanged;
        if (_map is not null) _map.PropertyChanged -= OnMapPropertyChanged;
        var shell = DataContext as MainWindowViewModel;
        _flight = shell?.Flight;
        _map = shell?.Map;
        if (_flight is null) return;
        _flight.PropertyChanged += OnFlightPropertyChanged;
        if (_map is not null) _map.PropertyChanged += OnMapPropertyChanged;
        UpdateGeometry();
        UpdateDistance();
    }

    private void OnFlightPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(FlightTelemetryViewModel.Altitude):
            case nameof(FlightTelemetryViewModel.Pitch):
            case nameof(FlightTelemetryViewModel.IsVehicleDataValid):
                UpdateGeometry();
                UpdateDistance();
                break;
            case nameof(FlightTelemetryViewModel.Latitude):
            case nameof(FlightTelemetryViewModel.Longitude):
                UpdateDistance();
                break;
        }
    }

    private void OnMapPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.QrTarget)) UpdateDistance();
    }

    /// <summary>
    /// Drone ikonunu ve bezier eğrisini canlı günceller.
    /// - Drone Y: irtifaya göre (alt yüksekse Y küçük yani yukarıda)
    /// - Bezier kontrol noktaları: pitch açısına göre eğrinin "bükümü" değişir
    ///   (dik dalış → daha keskin eğri, yatay → uzun yumuşak hat)
    /// </summary>
    private void UpdateGeometry()
    {
        if (_flight is null) return;
        if (!_flight.IsVehicleDataValid)
        {
            DroneIcon.Visibility = Visibility.Collapsed;
            ApproachCurve.Visibility = Visibility.Collapsed;
            return;
        }

        DroneIcon.Visibility = Visibility.Visible;
        ApproachCurve.Visibility = Visibility.Visible;

        // Drone Y: GroundY - alt*pix. Alt 100m → 80, 30m → 240, 0m → 280.
        var alt = Math.Max(0, Math.Min(AltitudeReference + 50, _flight.Altitude));
        var droneY = GroundY - alt * PixelsPerMeter;

        Canvas.SetTop(DroneIcon, droneY - 8);   // -8: ikon yarı yüksekliği için merkezi düzelt
        Canvas.SetLeft(DroneIcon, DroneStartX - 8);

        // Bezier başlangıç noktasını drone'la senkronize et.
        ApproachStart.StartPoint = new Point(DroneStartX, droneY);

        // Bezier kontrol noktaları — pitch'e göre eğri bükümü.
        // Dik pitch (-45° gibi) → ikinci control daha alçak (keskin dalış).
        // Düz pitch → kontroller yataya yakın.
        var pitchAbs = Math.Abs(_flight.Pitch);
        var bend = Math.Min(1.0, pitchAbs / 60.0); // 0..1
        var control1Y = droneY + (GroundY - droneY) * 0.15 + bend * 30;
        var control2Y = droneY + (GroundY - droneY) * 0.55 + bend * 50;
        var control1X = DroneStartX + (TargetX - DroneStartX) * 0.30;
        var control2X = DroneStartX + (TargetX - DroneStartX) * 0.70;

        ApproachBezier.Point1 = new Point(control1X, control1Y);
        ApproachBezier.Point2 = new Point(control2X, control2Y);
        ApproachBezier.Point3 = new Point(TargetX, GroundY);
    }

    /// <summary>
    /// DISTANCE TO TARGET — kendi konumumuzdan QR target'a haversine mesafe.
    /// Map.QrTarget yoksa "—" gösterir.
    /// </summary>
    private void UpdateDistance()
    {
        if (_flight?.IsVehicleDataValid != true || _map?.QrTarget is not { } target)
        {
            DistanceText.Text = "—";
            return;
        }

        // Inline haversine (GeoDistance internal — bu projeden erişim yok).
        const double R = 6_371_000;
        var lat1 = ToRad(_flight.Latitude);
        var lat2 = ToRad(target.Enlem);
        var dLat = ToRad(target.Enlem - _flight.Latitude);
        var dLng = ToRad(target.Boylam - _flight.Longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1) * Math.Cos(lat2)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var meters = R * c;
        DistanceText.Text = meters >= 1000 ? $"{meters / 1000:F1}k" : $"{meters:F0}";
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
