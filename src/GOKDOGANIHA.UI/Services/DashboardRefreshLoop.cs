using System.Windows.Threading;

namespace GOKDOGANIHA.UI.Services;

internal sealed class DashboardRefreshLoop : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly IReadOnlyList<Action<DateTime>> _refreshActions;

    public DashboardRefreshLoop(params Action<DateTime>[] refreshActions)
    {
        _refreshActions = refreshActions;
        _timer = new DispatcherTimer(DispatcherPriority.DataBind)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        RefreshAll();
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e) => RefreshAll();

    private void RefreshAll()
    {
        var nowUtc = DateTime.UtcNow;
        foreach (var refresh in _refreshActions)
            refresh(nowUtc);
    }
}
