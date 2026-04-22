using CommunityToolkit.Mvvm.ComponentModel;
using GOKDOGANIHA.Core.Configuration;

namespace GOKDOGANIHA.UI.ViewModels.Settings;

/// <summary>
/// TAKIM sekmesi — takım numarası (telemetri paketi için zorunlu) + çağrı işareti.
/// TakimNumarasi GameServerOptions'a bağlı; CallSign şimdilik yerel (tüketici UI only).
/// </summary>
public partial class TeamSettingsViewModel : OptionsBackedViewModel<GameServerOptions>
{
    public TeamSettingsViewModel() { }

    public TeamSettingsViewModel(GameServerOptions options) : base(options)
    {
        _teamNumber = options.TakimNumarasi;
    }

    [ObservableProperty] private int _teamNumber;
    [ObservableProperty] private string _callSign = "GÖKDOĞAN-1";

    partial void OnTeamNumberChanged(int value) => PushToOptions(o => o.TakimNumarasi = value);
    // CallSign: şimdilik UI display-only, Options'a bağlı değil.

    /// <summary>
    /// ServerSettingsViewModel.LoginSucceeded gibi dış event'lerden çağrılır —
    /// sunucu verilen authoritative takım numarasını VM'e yansıtmak için.
    /// </summary>
    public void ApplyRemoteTeamNumber(int teamNumber) => TeamNumber = teamNumber;
}
