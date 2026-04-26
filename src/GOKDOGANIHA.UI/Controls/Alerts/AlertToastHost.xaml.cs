using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.UI.Controls.Alerts;

/// <summary>
/// Sağ üst köşede 4 sn'lik popup uyarılar — `AlertBus.AlertPublished` event'ine
/// subscribe olur, her gelen alert için item ekler ve <see cref="DisplayDuration"/>
/// sonra otomatik kaldırır. Eski roadmap #6.
///
/// SOLID: kendi içinde encapsulated — VM bağımlılığı yok, sadece App.AlertBus
/// statik referansı (process-wide singleton, alternatif: DependencyProperty).
/// </summary>
public partial class AlertToastHost : UserControl
{
    /// <summary>Bir toast'ın ekranda kalma süresi.</summary>
    public TimeSpan DisplayDuration { get; set; } = TimeSpan.FromSeconds(4);

    public ObservableCollection<Alert> Toasts { get; } = new();

    public AlertToastHost()
    {
        InitializeComponent();
        ToastList.ItemsSource = Toasts;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App)
            App.AlertBus.AlertPublished += OnAlertPublished;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App)
            App.AlertBus.AlertPublished -= OnAlertPublished;
    }

    private void OnAlertPublished(object? sender, Alert alert)
    {
        // AlertBus arka thread'lerden tetiklenebilir; UI thread'e marshal et.
        var disp = Application.Current?.Dispatcher;
        if (disp is null || disp.CheckAccess()) ShowToast(alert);
        else disp.BeginInvoke(new Action(() => ShowToast(alert)));
    }

    private void ShowToast(Alert alert)
    {
        Toasts.Insert(0, alert);

        // 4 sn sonra otomatik sil. DispatcherTimer per-toast — basit, hafif.
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = DisplayDuration
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Toasts.Remove(alert);
        };
        timer.Start();
    }
}
