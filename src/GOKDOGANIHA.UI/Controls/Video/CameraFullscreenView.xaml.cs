using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GOKDOGANIHA.UI.Controls.Video;

public partial class CameraFullscreenView : UserControl
{
    private bool _wasShown;

    public CameraFullscreenView()
    {
        InitializeComponent();
        // Küçük panel ve tam ekran TEK paylaşılan RTSP oynatıcısını kullanır. LibVLC video
        // çıktısını yalnızca Play() anında bir pencereye bağlar; canlı Hwnd değişimi
        // görüntüyü taşımaz. Bu yüzden yüzey her değiştiğinde akışı yeniden başlatırız
        // (Restart → PlayCurrentUrl; Play'den önce UpdateActiveSurface o an görünür en
        // yüksek öncelikli yüzeyi hedefler), böylece görüntü doğru pencereye taşınır:
        //   • overlay AÇILINCA → video bu tam ekran yüzeye
        //   • overlay KAPANINCA → video küçük panele geri döner
        IsVisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            _wasShown = true;
            RestartToActiveSurface();
        }
        else if (_wasShown)
        {
            // Gerçek kapanış (açılıştan önceki startup false-geçişi değil): görüntüyü
            // küçük panele geri taşımak için akışı yeniden başlat.
            RestartToActiveSurface();
        }
    }

    private void RestartToActiveSurface() =>
        Dispatcher.BeginInvoke(new Action(() => FullscreenVideo.Restart()),
            DispatcherPriority.Loaded);
}
