using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.UI.ViewModels;
using GOKDOGANIHA.UI.ViewModels.Settings;

namespace GOKDOGANIHA.Tests.ViewModels;

/// <summary>
/// Faz 1 korumalı: sub-VM'ler Options'a gerçekten push ediyor mu ve
/// seed ctor'ları Options'tan gerçekten okuyor mu?
/// </summary>
public class OptionsSyncTests
{
    [Fact]
    public void ServerSettings_pushes_BaseUrl_to_options()
    {
        var opts = new GameServerOptions { BaseUrl = "http://a" };
        var vm = new ServerSettingsViewModel(opts);

        vm.ServerBaseUrl = "http://b";

        Assert.Equal("http://b", opts.BaseUrl);
    }

    [Fact]
    public void ServerSettings_seeds_VM_from_options()
    {
        var opts = new GameServerOptions { BaseUrl = "http://seed", KullaniciAdi = "u", Sifre = "p" };
        var vm = new ServerSettingsViewModel(opts);
        Assert.Equal("http://seed", vm.ServerBaseUrl);
        Assert.Equal("u", vm.TeamUsername);
        Assert.Equal("p", vm.TeamPassword);
    }

    [Fact]
    public void Test_profile_switches_default_url_to_local_mock()
    {
        var opts = new GameServerOptions();
        var vm = new ServerSettingsViewModel(opts);

        vm.UseTestEnvironment = true;

        Assert.Equal(CompetitionServerEnvironment.Test, opts.Environment);
        Assert.Equal("http://127.0.0.1:5000", opts.BaseUrl);
    }

    [Fact]
    public void TeamSettings_pushes_TeamNumber_to_GameServerOptions()
    {
        var opts = new GameServerOptions();
        var vm = new TeamSettingsViewModel(opts);

        vm.TeamNumber = 42;

        Assert.Equal(42, opts.TakimNumarasi);
    }

    [Fact]
    public void AlertSettings_pushes_all_thresholds()
    {
        var opts = new AlertOptions();
        var vm = new AlertSettingsViewModel(opts)
        {
            LowBatteryThreshold = 20,
            OpponentProximityThreshold = 600,
            HssProximityThreshold = 75,
            BoundaryProximityThreshold = 120,
            CommLatencyThreshold = 800
        };

        Assert.Equal(20, opts.LowBatteryThreshold);
        Assert.Equal(600, opts.OpponentProximityThreshold);
        Assert.Equal(75, opts.HssProximityThreshold);
        Assert.Equal(120, opts.BoundaryProximityThreshold);
        Assert.Equal(800, opts.CommLatencyThreshold);
    }

    [Fact]
    public void TelemetrySettings_pushes_Mavlink_fields_to_options()
    {
        var opts = new TelemetryOptions();
        var mavlink = new MavlinkOptions();

        var vm = new TelemetrySettingsViewModel(opts, mavlink);
        vm.MavlinkListenAddress = "127.0.0.1";
        vm.MavlinkPort = 14551;
        vm.MavlinkTransport = MavlinkTransport.Serial;
        vm.MavlinkSerialPortName = "COM8";
        vm.MavlinkBaudRate = 115200;
        vm.MavlinkExpectedSystemId = 7;

        Assert.Equal("127.0.0.1", mavlink.ListenAddress);
        Assert.Equal(14551, mavlink.Port);
        Assert.Equal(MavlinkTransport.Serial, mavlink.Transport);
        Assert.Equal("COM8", mavlink.SerialPortName);
        Assert.Equal(115200, mavlink.BaudRate);
        Assert.Equal(7, mavlink.ExpectedSystemId);
    }

    [Fact]
    public void TelemetrySettings_seeds_Mavlink_fields_from_options()
    {
        var opts = new TelemetryOptions();
        var mavlink = new MavlinkOptions { ListenAddress = "127.0.0.2", Port = 16000 };
        var vm = new TelemetrySettingsViewModel(opts, mavlink);
        Assert.Equal("127.0.0.2", vm.MavlinkListenAddress);
        Assert.Equal(16000, vm.MavlinkPort);
    }

    [Fact]
    public void SubVMs_with_no_options_stay_silent()
    {
        // Parametresiz ctor — Options null, PushToOptions no-op.
        var vm = new TelemetrySettingsViewModel();
        vm.Hz = 1.7;
        // Hiçbir exception atılmadı → pattern çalışıyor
        Assert.Equal(1.7, vm.Hz);
    }

    [Fact]
    public void SettingsViewModel_aggregator_wires_sub_VMs_from_ApplicationOptions()
    {
        var app = new ApplicationOptions();
        var settings = new SettingsViewModel(app);

        settings.Server.ServerBaseUrl = "http://new";
        settings.Team.TeamNumber = 7;
        settings.Telemetry.Hz = 1.5;
        settings.Map.ShowGrid = false;
        settings.Alerts.LowBatteryThreshold = 19;

        Assert.Equal("http://new", app.GameServer.BaseUrl);
        Assert.Equal(7, app.GameServer.TakimNumarasi);
        Assert.Equal(1.5, app.Telemetry.Hz);
        Assert.False(app.Map.ShowGrid);
        Assert.Equal(19, app.Alerts.LowBatteryThreshold);
    }
}
