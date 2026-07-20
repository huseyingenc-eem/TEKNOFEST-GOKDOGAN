using System.Windows;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Map.Layers;

internal sealed class TrailLayerRenderer
{
    private static readonly TimeSpan FullyVisibleAge = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMinutes(2);
    private const double MinimumOpacity = 0.08;
    private const int FadeBuckets = 16;

    private readonly GMapControl _map;
    private readonly FrameworkElement _resources;
    private readonly List<GMapRoute> _routes = [];

    public TrailLayerRenderer(GMapControl map, FrameworkElement resources)
    {
        _map = map;
        _resources = resources;
    }

    public bool HasRoutes => _routes.Count > 0;

    public void Rebuild(MapViewModel viewModel, DateTime nowUtc)
    {
        Clear();
        if (!viewModel.ShowOwnTrail || viewModel.OwnTrail.Count < 2) return;

        foreach (var segment in BuildFadeSegments(viewModel.GetOwnTrailSamples(), nowUtc))
        {
            var haloRoute = CreateRoute(segment.Points, "trail-halo", 198);
            var coreRoute = CreateRoute(segment.Points, "trail", 199);
            Style(haloRoute, coreRoute, segment.Opacity);
        }
    }

    private GMapRoute CreateRoute(List<PointLatLng> points, string tag, int zIndex)
    {
        var route = new GMapRoute(points) { Tag = tag, ZIndex = zIndex };
        _routes.Add(route);
        _map.Markers.Add(route);
        return route;
    }

    private void Style(GMapRoute haloRoute, GMapRoute coreRoute, double opacity)
    {
        var accent = (Brush)_resources.FindResource("TacticalAccent");
        var halo = new SolidColorBrush(Color.FromArgb(210, 3, 12, 17));
        MapShapeStyler.Apply(
            haloRoute.Shape, halo, 6.5, 0.82 * opacity, null, dashed: false);
        MapShapeStyler.Apply(
            coreRoute.Shape, accent, 2.4, 0.96 * opacity, null, dashed: false);
    }

    private void Clear()
    {
        foreach (var route in _routes) _map.Markers.Remove(route);
        _routes.Clear();
    }

    private static IReadOnlyList<TrailFadeSegment> BuildFadeSegments(
        IReadOnlyList<OwnTrailSample> samples,
        DateTime nowUtc)
    {
        if (samples.Count < 2) return [];

        var result = new List<TrailFadeSegment>();
        var currentPoints = new List<PointLatLng>();
        var currentBucket = -1;

        foreach (var sample in samples)
        {
            var opacity = CalculateOpacity(nowUtc - sample.RecordedUtc);
            var bucket = Math.Clamp(
                (int)Math.Round(
                    (opacity - MinimumOpacity) / (1 - MinimumOpacity) * (FadeBuckets - 1)),
                0,
                FadeBuckets - 1);

            if (sample.StartsNewSegment && currentPoints.Count > 0)
            {
                AddSegment(result, currentPoints, currentBucket);
                currentPoints = [];
                currentBucket = -1;
            }
            else if (currentBucket != -1 && bucket != currentBucket)
            {
                AddSegment(result, currentPoints, currentBucket);
                currentPoints = [currentPoints[^1]];
            }

            currentBucket = bucket;
            currentPoints.Add(sample.Position);
        }

        AddSegment(result, currentPoints, currentBucket);
        return result;
    }

    private static void AddSegment(
        ICollection<TrailFadeSegment> result,
        List<PointLatLng> points,
        int bucket)
    {
        if (points.Count < 2 || bucket < 0) return;
        var opacity = MinimumOpacity
                      + bucket / (double)(FadeBuckets - 1) * (1 - MinimumOpacity);
        result.Add(new TrailFadeSegment(points, opacity));
    }

    private static double CalculateOpacity(TimeSpan age)
    {
        if (age <= FullyVisibleAge) return 1;
        var fadeProgress = (age - FullyVisibleAge).TotalMilliseconds
                           / FadeDuration.TotalMilliseconds;
        return 1 - Math.Clamp(fadeProgress, 0, 1) * (1 - MinimumOpacity);
    }

    private sealed record TrailFadeSegment(List<PointLatLng> Points, double Opacity);
}
