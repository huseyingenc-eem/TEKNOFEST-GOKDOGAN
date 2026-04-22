namespace GOKDOGANIHA.UI.ViewModels;

/// <summary>
/// XAML designer için tamamen static / unwired SettingsViewModel. Gerçek
/// Options yok, sync yok — ayarlara tıklamak pencereyi çökmez.
/// Kullanım: <c>d:DataContext="{d:DesignInstance vm:DesignTimeSettingsViewModel, IsDesignTimeCreatable=True}"</c>
/// </summary>
public sealed class DesignTimeSettingsViewModel : SettingsViewModel
{
    public DesignTimeSettingsViewModel() : base()
    {
        // Realistic design-time defaults (already set by sub-VM defaults).
        // Buraya ek olarak "seed" edilecek design-time değerler eklenebilir:
        Team.CallSign = "GÖKDOĞAN-DEMO";
        Team.TeamNumber = 42;
    }
}
