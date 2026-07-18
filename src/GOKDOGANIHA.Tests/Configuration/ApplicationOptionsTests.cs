using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.Tests.Configuration;

public class ApplicationOptionsTests
{
    [Fact]
    public void Defaults_match_sartname_safe_values()
    {
        var opts = new ApplicationOptions();

        Assert.Equal("http://127.0.0.25:5000", opts.GameServer.BaseUrl);
        Assert.Equal(1.0, opts.Telemetry.Hz);                // şartname: max 2 Hz
        Assert.True(opts.Telemetry.AutoReconnect);
        Assert.Equal("StadiaAlidadeSmoothDark", opts.Map.TileProvider);
        Assert.True(opts.Map.ShowBoundary);
        Assert.Equal(22, opts.Alerts.LowBatteryThreshold);
        Assert.Equal(500, opts.Alerts.OpponentProximityThreshold);
        Assert.Equal(50, opts.Alerts.HssProximityThreshold);
    }

    [Fact]
    public void Child_options_are_distinct_instances()
    {
        var a = new ApplicationOptions();
        var b = new ApplicationOptions();
        Assert.NotSame(a.GameServer, b.GameServer);
        Assert.NotSame(a.Telemetry, b.Telemetry);
        Assert.NotSame(a.Mavlink, b.Mavlink);
    }
}
