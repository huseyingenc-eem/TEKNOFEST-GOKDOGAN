using System;
using System.Windows;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.UI;

public partial class App : Application
{
    // Composition root — tek bir ApplicationOptions instance'ı tüm servisleri ve
    // sub-VM'leri besler. Doğrudan statik erişim şu an zaruri; ileride
    // Microsoft.Extensions.DependencyInjection'a geçiş kolay.
    public static ApplicationOptions AppOptions { get; } = new();

    // Geriye uyumluluk için kısayol — yeni kod AppOptions.GameServer kullansın.
    public static GameServerOptions ServerOptions => AppOptions.GameServer;

    public static IGameServerClient? GameServer { get; private set; }
    public static TelemetryPollService? TelemetryPoll { get; private set; }
    public static HssPollService? HssPoll { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Geliştirme sırasında: stack'i MessageBox ile göster. Yarışma build'inde
        // log file'a yönlendirilmeli.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Unhandled UI exception:\n\n{args.Exception}",
                "GÖKDOĞAN — Crash",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Unhandled domain exception:\n\n{args.ExceptionObject}",
                "GÖKDOĞAN — Crash",
                MessageBoxButton.OK, MessageBoxImage.Error);
        };

        base.OnStartup(e);
        try
        {
            var client = new GameServerClient(AppOptions.GameServer);
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
