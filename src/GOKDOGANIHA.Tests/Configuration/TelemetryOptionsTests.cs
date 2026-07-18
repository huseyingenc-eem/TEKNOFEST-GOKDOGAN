using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.Tests.Configuration;

public class TelemetryOptionsTests
{
    [Theory]
    [InlineData(0.1, 1.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(1.5, 1.5)]
    [InlineData(2.0, 2.0)]
    [InlineData(5.0, 2.0)]
    public void Hz_is_clamped_to_documented_range(double requested, double expected)
    {
        var options = new TelemetryOptions { Hz = requested };
        Assert.Equal(expected, options.Hz);
    }
}
