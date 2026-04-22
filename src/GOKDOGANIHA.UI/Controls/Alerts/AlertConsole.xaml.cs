using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.UI.Controls.Alerts;

public partial class AlertConsole : UserControl
{
    // Max 100 — eski'leri düşürerek log listesini bounded tut.
    private const int MaxEntries = 100;
    private readonly ObservableCollection<AlertRow> _rows = new();

    public AlertConsole()
    {
        InitializeComponent();
        AlertItems.ItemsSource = _rows;

        // AlertBus App tarafından Loaded event'inde bağlanır.
        Loaded += (_, _) =>
        {
            if (App.AlertBus is not null) SubscribeTo(App.AlertBus);
        };
        Unloaded += (_, _) =>
        {
            if (App.AlertBus is not null) UnsubscribeFrom(App.AlertBus);
        };
    }

    public void SubscribeTo(IAlertSubscriber bus)
    {
        bus.AlertPublished += OnAlertPublished;
    }

    public void UnsubscribeFrom(IAlertSubscriber bus)
    {
        bus.AlertPublished -= OnAlertPublished;
    }

    private void OnAlertPublished(object? sender, Alert alert)
    {
        // Bus arbitrary thread'den publish edebilir; UI thread'ine marshal et.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _rows.Add(AlertRow.From(alert));
            while (_rows.Count > MaxEntries) _rows.RemoveAt(0);
            Scroller.ScrollToBottom();
        }));
    }

    /// <summary>XAML binding için düzleştirilmiş row view model.</summary>
    private sealed record AlertRow(string TimeLabel, string Tag, string Message, Brush LevelBrush)
    {
        public static AlertRow From(Alert a)
        {
            var tag = $"[{a.Kind.ToUpperInvariant()}]";
            var time = a.TimeUtc.ToLocalTime().ToString("HH:mm:ss");
            var brush = a.Level switch
            {
                AlertLevel.Info => (Brush)System.Windows.Application.Current.FindResource("TacticalAccent"),
                AlertLevel.Warn => (Brush)System.Windows.Application.Current.FindResource("TacticalWarn"),
                AlertLevel.Danger => (Brush)System.Windows.Application.Current.FindResource("TacticalCritical"),
                _ => Brushes.Gray
            };
            return new AlertRow(time, tag, $"— {a.Title}: {a.Message}", brush);
        }
    }
}
