using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GOKDOGANIHA.Core.Models.Connection;

namespace GOKDOGANIHA.UI.Core;

/// <summary>
/// Bir <see cref="ConnectionStatus"/>'u tutarlı biçimde gösteren yeniden kullanılabilir
/// rozet: faz→renk noktası + ad + canlı durum metni + retry sayacı + (Faulted'da)
/// Tekrar Dene. Sunucu, telemetri ve video aynı kontrolü kullanır.
/// </summary>
public partial class ConnectionStatusPill : UserControl
{
    public ConnectionStatusPill() => InitializeComponent();

    /// <summary>Gösterilecek bağlantı durumu. INotifyPropertyChanged üzerinden canlı güncellenir.</summary>
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(ConnectionStatus), typeof(ConnectionStatusPill),
            new PropertyMetadata(null));

    public ConnectionStatus? Status
    {
        get => (ConnectionStatus?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>Faulted durumunda "Tekrar Dene" ile çalıştırılacak komut. Verilmezse buton gizli.</summary>
    public static readonly DependencyProperty RetryCommandProperty =
        DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(ConnectionStatusPill),
            new PropertyMetadata(null));

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    /// <summary>
    /// True olduğunda Faulted'a ek olarak Connecting/Retrying fazlarında da kullanıcı
    /// kaynağı elle yeniden başlatabilir. MAVLink veri bekleme durumu için kullanılır.
    /// </summary>
    public static readonly DependencyProperty ShowRetryWhileBusyProperty =
        DependencyProperty.Register(nameof(ShowRetryWhileBusy), typeof(bool), typeof(ConnectionStatusPill),
            new PropertyMetadata(false));

    public bool ShowRetryWhileBusy
    {
        get => (bool)GetValue(ShowRetryWhileBusyProperty);
        set => SetValue(ShowRetryWhileBusyProperty, value);
    }
}
