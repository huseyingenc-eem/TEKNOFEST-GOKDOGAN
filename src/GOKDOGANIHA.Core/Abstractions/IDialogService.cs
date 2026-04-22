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
}
