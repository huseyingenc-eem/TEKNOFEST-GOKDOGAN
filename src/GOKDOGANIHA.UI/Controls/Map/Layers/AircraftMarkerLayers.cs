using System.Windows;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.UI.Controls.Map.Markers;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Controls.Map.Layers;

internal sealed class EnemyDroneLayer
{
    private readonly GMapControl _map;
    private readonly Dictionary<int, GMapMarker> _markers = [];

    public EnemyDroneLayer(GMapControl map) => _map = map;

    public void Rebuild(IEnumerable<KonumBilgisi> opponents)
    {
        foreach (var marker in _markers.Values) _map.Markers.Remove(marker);
        _markers.Clear();

        foreach (var opponent in opponents)
        {
            var visual = new EnemyDroneMarker
            {
                TeamNumber = opponent.TakimNumarasi,
                HeadingAngle = opponent.Yonelme
            };
            var marker = new GMapMarker(new PointLatLng(opponent.Enlem, opponent.Boylam))
            {
                Shape = visual,
                Offset = new Point(-20, -27)
            };
            _map.Markers.Add(marker);
            _markers[opponent.TakimNumarasi] = marker;
        }
    }
}

internal sealed class OwnshipLayer
{
    private readonly GMapControl _map;
    private GMapMarker? _marker;
    private OwnDroneMarker? _visual;

    public OwnshipLayer(GMapControl map) => _map = map;

    public void Update(MapViewModel viewModel)
    {
        if (!viewModel.HasOwnPosition)
        {
            Remove();
            return;
        }

        if (_marker is null)
        {
            _visual = new OwnDroneMarker();
            _marker = new GMapMarker(new PointLatLng(viewModel.OwnLatitude, viewModel.OwnLongitude))
            {
                Shape = _visual,
                Offset = new Point(-22, -22),
                ZIndex = 500
            };
            _map.Markers.Add(_marker);
        }

        _marker.Position = new PointLatLng(viewModel.OwnLatitude, viewModel.OwnLongitude);
        if (_visual is not null) _visual.HeadingAngle = viewModel.OwnHeading;
        if (viewModel.FollowOwnship) _map.Position = _marker.Position;
    }

    private void Remove()
    {
        if (_marker is null) return;
        _map.Markers.Remove(_marker);
        _marker = null;
        _visual = null;
    }
}

internal sealed class QrTargetLayer
{
    private readonly GMapControl _map;
    private GMapMarker? _marker;

    public QrTargetLayer(GMapControl map) => _map = map;

    public void Update(QrKoordinat? target)
    {
        if (target is null)
        {
            if (_marker is not null) _map.Markers.Remove(_marker);
            _marker = null;
            return;
        }

        _marker ??= new GMapMarker(new PointLatLng(target.Enlem, target.Boylam))
        {
            Shape = new QrTargetMarker(),
            Offset = new Point(-17, -17),
            ZIndex = 400
        };
        _marker.Position = new PointLatLng(target.Enlem, target.Boylam);
        if (!_map.Markers.Contains(_marker)) _map.Markers.Add(_marker);
    }
}
