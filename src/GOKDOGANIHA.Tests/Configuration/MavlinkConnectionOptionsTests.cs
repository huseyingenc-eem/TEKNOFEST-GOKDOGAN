using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.Tests.Configuration;

public class MavlinkConnectionOptionsTests
{
    [Fact]
    public void Serial_defaults_match_common_RFD_8N1_profile()
    {
        var options = new MavlinkOptions { Transport = MavlinkTransport.Serial };

        Assert.Equal("COM3", options.SerialPortName);
        Assert.Equal(57600, options.BaudRate);
        Assert.True(options.AutoReconnect);
        Assert.Contains("57600", options.ConnectionDescription);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(7, 7)]
    [InlineData(300, 255)]
    public void Expected_system_id_is_clamped(int requested, int expected)
    {
        var options = new MavlinkOptions { ExpectedSystemId = requested };
        Assert.Equal(expected, options.ExpectedSystemId);
    }
}
