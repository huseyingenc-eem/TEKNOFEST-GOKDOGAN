using System.Threading.Tasks;

namespace GOKDOGANIHA.Core.Abstractions;

/// <summary>
/// VM'lerin MessageBox gibi UI primitif'lerine doğrudan bağımlı olmasını
/// önlemek için soyutlama. WPF implementasyonu UI katmanında.
/// </summary>
public interface IDialogService
{
    Task ShowInfoAsync(string title, string message);
    Task ShowWarnAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Yes/No onay diyaloğu. Safety-critical aksiyonlar (ör. uçuşta DISARM)
    /// için kullanılır. Kullanıcı "Evet" → true, "Hayır" / kapat → false.
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message, string yesText = "Evet", string noText = "İptal");
}
