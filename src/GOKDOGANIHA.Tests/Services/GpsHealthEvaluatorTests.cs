using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Tests.Services;

public class GpsHealthEvaluatorTests
{
    [Theory]
    [InlineData(false, GpsFix.Fix3D, 14, 0.9, GpsHealth.Unavailable)]
    [InlineData(true, GpsFix.Fix2D, 14, 0.9, GpsHealth.Critical)]
    [InlineData(true, GpsFix.Fix3D, 6, 0.9, GpsHealth.Critical)]
    [InlineData(true, GpsFix.Fix3D, 14, 2.1, GpsHealth.Critical)]
    [InlineData(true, GpsFix.Fix3D, 10, 1.2, GpsHealth.Warning)]
    [InlineData(true, GpsFix.Fix3D, 14, 1.8, GpsHealth.Warning)]
    [InlineData(true, GpsFix.Fix3D, 14, null, GpsHealth.Warning)]
    [InlineData(true, GpsFix.Fix3D, 14, 0.9, GpsHealth.Healthy)]
    public void Combines_fix_satellites_and_hdop(
        bool valid,
        GpsFix fix,
        int satellites,
        double? hdop,
        GpsHealth expected)
    {
        Assert.Equal(expected, GpsHealthEvaluator.Evaluate(valid, fix, satellites, hdop));
    }
}
