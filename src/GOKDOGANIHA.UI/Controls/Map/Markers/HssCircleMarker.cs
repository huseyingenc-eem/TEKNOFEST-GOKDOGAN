using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GOKDOGANIHA.UI.Controls.Map.Markers;

// Grid wrapper around an Ellipse so we can size it in pixels derived from a
// metric radius at a given latitude and map zoom. Ellipse itself is sealed in
// WPF, so we compose rather than inherit.
public sealed class HssCircleMarker : Grid
{
    private const double EarthCircumferenceMeters = 40_075_016.686;
    private const double TileSize = 256.0;

    private readonly Ellipse _ellipse;

    public int HssId { get; }

    public HssCircleMarker(int hssId, Brush stroke, Brush fill)
    {
        HssId = hssId;
        IsHitTestVisible = false;
        _ellipse = new Ellipse
        {
            Stroke = stroke,
            StrokeThickness = 2,
            Fill = fill,
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
