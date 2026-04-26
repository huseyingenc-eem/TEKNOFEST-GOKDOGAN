using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GOKDOGANIHA.UI.Controls.Map.Markers;

/// <summary>
/// Sinyal karıştırma bölgesi — mor renkte, dashed stroke ile HSS'ten ayırt edilir.
/// HssCircleMarker ile aynı zoom-aware metric→pixel hesabı (DRY için ortak helper
/// yerine küçük bir kopya — Marker class'ları zaten ContentControl-light).
/// </summary>
public sealed class JammingCircleMarker : Grid
{
    private const double EarthCircumferenceMeters = 40_075_016.686;
    private const double TileSize = 256.0;

    private readonly Ellipse _ellipse;

    public int ZoneId { get; }

    public JammingCircleMarker(int zoneId)
    {
        ZoneId = zoneId;
        IsHitTestVisible = false;
        _ellipse = new Ellipse
        {
            Stroke = new SolidColorBrush(Color.FromRgb(0x9B, 0x59, 0xB6)),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Fill = new SolidColorBrush(Color.FromArgb(0x22, 0x9B, 0x59, 0xB6)),
            IsHitTestVisible = false
        };
        Children.Add(_ellipse);
    }

    public void SetRadius(double meters, double latitudeDeg, int zoom)
    {
        double metersPerPixel = EarthCircumferenceMeters
            * Math.Cos(latitudeDeg * Math.PI / 180.0)
            / (TileSize * Math.Pow(2, zoom));
        double diameter = Math.Max(2, 2 * meters / metersPerPixel);
        Width = diameter;
        Height = diameter;
        _ellipse.Width = diameter;
        _ellipse.Height = diameter;
    }
}
