using System.Threading.Tasks;
using System.Windows;
using GOKDOGANIHA.Core.Abstractions;

namespace GOKDOGANIHA.UI.Services;

/// <summary>
/// WPF MessageBox tabanlı IDialogService implementasyonu. UI thread'e marshal
/// etmiyor; çağıran zaten UI thread'de olur (VM command UI context'ine geri döner).
/// Task.CompletedTask döner — MessageBox synchronous, API async sadece soyutlama için.
/// </summary>
public sealed class DialogService : IDialogService
{
    public Task ShowInfoAsync(string title, string message) => Show(title, message, MessageBoxImage.Information);
    public Task ShowWarnAsync(string title, string message) => Show(title, message, MessageBoxImage.Warning);
    public Task ShowErrorAsync(string title, string message) => Show(title, message, MessageBoxImage.Error);

    private static Task Show(string title, string message, MessageBoxImage icon)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        return Task.CompletedTask;
    }
}
