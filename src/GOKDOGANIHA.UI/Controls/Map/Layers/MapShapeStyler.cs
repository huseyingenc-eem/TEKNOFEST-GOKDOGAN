using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GOKDOGANIHA.UI.Controls.Map.Layers;

internal static class MapShapeStyler
{
    public static void Apply(
        UIElement? shape,
        Brush stroke,
        double thickness,
        double opacity,
        Brush? fill,
        bool dashed,
        DoubleCollection? dashArray = null)
    {
        var dashes = dashed ? (dashArray ?? new DoubleCollection { 4, 3 }) : null;

        switch (shape)
        {
            case Path path:
                path.Stroke = stroke;
                path.StrokeThickness = thickness;
                path.Opacity = opacity;
                path.Fill = fill;
                path.StrokeDashArray = dashes;
                path.StrokeStartLineCap = PenLineCap.Round;
                path.StrokeEndLineCap = PenLineCap.Round;
                path.StrokeLineJoin = PenLineJoin.Round;
                path.IsHitTestVisible = false;
                break;
            case Polygon polygon:
                polygon.Stroke = stroke;
                polygon.StrokeThickness = thickness;
                polygon.Opacity = opacity;
                polygon.Fill = fill;
                polygon.StrokeDashArray = dashes;
                polygon.StrokeLineJoin = PenLineJoin.Round;
                polygon.IsHitTestVisible = false;
                break;
        }
    }
}
