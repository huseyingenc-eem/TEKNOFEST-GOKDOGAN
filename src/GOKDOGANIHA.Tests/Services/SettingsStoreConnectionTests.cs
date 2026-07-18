using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Persistence;

namespace GOKDOGANIHA.Tests.Services;

public class SettingsStoreConnectionTests
{
    [Fact]
    public void Serial_mavlink_and_server_settings_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gokdogan-settings-{Guid.NewGuid():N}.json");
        try
        {
            var original = new ApplicationOptions();
            original.GameServer.BaseUrl = "http://10.10.10.25:5000";
            original.GameServer.KullaniciAdi = "team";
            original.Mavlink.Transport = MavlinkTransport.Serial;
            original.Mavlink.SerialPortName = "COM8";
            original.Mavlink.BaudRate = 115200;
            original.Mavlink.ExpectedSystemId = 7;
            original.Mavlink.AutoReconnect = false;

            var store = new SettingsStore();
            store.Save(original, path);
            var loaded = store.Load(path);

            Assert.Equal("http://10.10.10.25:5000", loaded.GameServer.BaseUrl);
            Assert.Equal("team", loaded.GameServer.KullaniciAdi);
            Assert.Equal(MavlinkTransport.Serial, loaded.Mavlink.Transport);
            Assert.Equal("COM8", loaded.Mavlink.SerialPortName);
            Assert.Equal(115200, loaded.Mavlink.BaudRate);
            Assert.Equal(7, loaded.Mavlink.ExpectedSystemId);
            Assert.False(loaded.Mavlink.AutoReconnect);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
