using System;
using System.Windows;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.UI;

public partial class App : Application
{
    // Runtime service holders. Services are constructed but NOT started here —
    // poll timers stay idle until login/connect flow calls Start(). This keeps
    // the app runnable offline (boundary polygon + OSM tiles visible) without
    // flooding the log with failed HTTP attempts.
    public static GameServerOptions ServerOptions { get; } = new();
    public static IGameServerClient? GameServer { get; private set; }
    public static TelemetryPollService? TelemetryPoll { get; private set; }
    public static HssPollService? HssPoll { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var client = new GameServerClient(ServerOptions);
            GameServer = client;
            TelemetryPoll = new TelemetryPollService(client);
            HssPoll = new HssPollService(client);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Service bootstrap failed: {ex.Message}");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TelemetryPoll?.Dispose();
        HssPoll?.Dispose();
        (GameServer as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}
