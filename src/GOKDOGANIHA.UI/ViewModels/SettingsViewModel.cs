using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.UI.ViewModels.Settings;

namespace GOKDOGANIHA.UI.ViewModels;

/// <summary>
/// Settings overlay'ının root ViewModel'i. Hiçbir state'i kendisi tutmaz;
/// her sekmenin sub-VM'sini property olarak expose eder (aggregator pattern).
/// XAML binding path'leri: Settings.Server.ServerBaseUrl, Settings.Team.CallSign, vb.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    // Parametresiz: design-time / unit test. Tüm sub-VM'ler default state'li.
    public SettingsViewModel()
    {
        Server = new ServerSettingsViewModel();
        Team = new TeamSettingsViewModel();
        Telemetry = new TelemetrySettingsViewModel();
        Video = new VideoSettingsViewModel();
        Map = new MapSettingsViewModel();
        Alerts = new AlertSettingsViewModel();
        Geofence = new GeofenceSettingsViewModel();
        Failsafe = new FailsafeSettingsViewModel();
        Autonomy = new AutonomySettingsViewModel();
    }

    // Runtime: tüm Options'lar root ApplicationOptions'dan gelir.
    public SettingsViewModel(
        ApplicationOptions app,
        IGameServerClient? gameServer = null,
        IDialogService? dialog = null)
    {
        Server = new ServerSettingsViewModel(app.GameServer, gameServer, dialog);
        Team = new TeamSettingsViewModel(app.GameServer);
        Telemetry = new TelemetrySettingsViewModel(app.Telemetry);
        Video = new VideoSettingsViewModel(app.Video);
        Map = new MapSettingsViewModel(app.Map);
        Alerts = new AlertSettingsViewModel(app.Alerts);
        Geofence = new GeofenceSettingsViewModel(app.Geofence);
        Failsafe = new FailsafeSettingsViewModel(app.Failsafe);
        Autonomy = new AutonomySettingsViewModel(app.Autonomy);

        // Login başarılı olunca sunucu takım numarasını Team sekmesine aktar.
        Server.LoginSucceeded += (_, teamNumber) => Team.ApplyRemoteTeamNumber(teamNumber);
    }

    public ServerSettingsViewModel Server { get; }
    public TeamSettingsViewModel Team { get; }
    public TelemetrySettingsViewModel Telemetry { get; }
    public VideoSettingsViewModel Video { get; }
    public MapSettingsViewModel Map { get; }
    public AlertSettingsViewModel Alerts { get; }
    public GeofenceSettingsViewModel Geofence { get; }
    public FailsafeSettingsViewModel Failsafe { get; }
    public AutonomySettingsViewModel Autonomy { get; }
}
