using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using GOKDOGANIHA.Core.Models.Connection;

namespace GOKDOGANIHA.UI.Controls.Video;

/// <summary>
/// RTSP akışını gösteren WPF video yüzeyi.
/// Aynı <see cref="SessionKey"/> değerini kullanan yüzeyler tek bir LibVLC
/// oynatıcısını paylaşır; böylece küçük/tam ekran geçişinde RTSP
/// bağlantısı yeniden kurulmaz.
/// </summary>
public sealed class RtspVideoView : VideoView
{
    private const string PlayerHostPartName = "PART_PlayerHost";

    // LibVLCSharp.WPF 3.x foreground HUD penceresini Visibility değişiminde
    // gizlemiyor; yalnızca HwndHost Loaded/Unloaded olaylarını izliyor.
    private static readonly PropertyInfo? ForegroundWindowProperty = typeof(VideoView).GetProperty(
        "ForegroundWindow",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly DependencyProperty StreamUrlProperty = DependencyProperty.Register(
        nameof(StreamUrl),
        typeof(string),
        typeof(RtspVideoView),
        new PropertyMetadata(string.Empty, OnPlaybackOptionChanged));

    public static readonly DependencyProperty NetworkCachingMsProperty = DependencyProperty.Register(
        nameof(NetworkCachingMs),
        typeof(int),
        typeof(RtspVideoView),
        new PropertyMetadata(150, OnPlaybackOptionChanged, CoerceNetworkCaching));

    public static readonly DependencyProperty UseTcpProperty = DependencyProperty.Register(
        nameof(UseTcp),
        typeof(bool),
        typeof(RtspVideoView),
        new PropertyMetadata(true, OnPlaybackOptionChanged));

    public static readonly DependencyProperty SessionKeyProperty = DependencyProperty.Register(
        nameof(SessionKey),
        typeof(string),
        typeof(RtspVideoView),
        new PropertyMetadata(string.Empty, OnSessionKeyChanged));

    public static readonly DependencyProperty SurfacePriorityProperty = DependencyProperty.Register(
        nameof(SurfacePriority),
        typeof(int),
        typeof(RtspVideoView),
        new PropertyMetadata(0, OnSurfacePriorityChanged));

    /// <summary>
    /// Opsiyonel ortak bağlantı durumu köprüsü. Bağlanınca video yüzeyinin durumu
    /// (bağlanıyor/canlı/kesildi) TopBar'daki VİDEO rozetiyle paylaşılır.
    /// </summary>
    public static readonly DependencyProperty VideoStatusProperty = DependencyProperty.Register(
        nameof(VideoStatus),
        typeof(ConnectionStatus),
        typeof(RtspVideoView),
        new PropertyMetadata(null));

    public ConnectionStatus? VideoStatus
    {
        get => (ConnectionStatus?)GetValue(VideoStatusProperty);
        set => SetValue(VideoStatusProperty, value);
    }

    private static readonly DependencyPropertyKey StatusTextPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(StatusText),
            typeof(string),
            typeof(RtspVideoView),
            new PropertyMetadata("AKIŞ BEKLENİYOR"));

    public static readonly DependencyProperty StatusTextProperty = StatusTextPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsStreamConnectedPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsStreamConnected),
            typeof(bool),
            typeof(RtspVideoView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsStreamConnectedProperty =
        IsStreamConnectedPropertyKey.DependencyProperty;

    private readonly string _privateSessionKey = $"RtspVideoView:{Guid.NewGuid():N}";
    private SharedRtspSession? _session;
    private bool _disposed;

    public RtspVideoView()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    public string StreamUrl
    {
        get => (string)GetValue(StreamUrlProperty);
        set => SetValue(StreamUrlProperty, value);
    }

    public int NetworkCachingMs
    {
        get => (int)GetValue(NetworkCachingMsProperty);
        set => SetValue(NetworkCachingMsProperty, value);
    }

    public bool UseTcp
    {
        get => (bool)GetValue(UseTcpProperty);
        set => SetValue(UseTcpProperty, value);
    }

    /// <summary>
    /// Aynı anahtara sahip video yüzeyleri aynı decoder ve RTSP bağlantısını kullanır.
    /// Boş bırakılırsa yüzey için özel bir oturum oluşturulur.
    /// </summary>
    public string SessionKey
    {
        get => (string)GetValue(SessionKeyProperty);
        set => SetValue(SessionKeyProperty, value);
    }

    /// <summary>
    /// Birden fazla paylaşılan yüzey aynı anda görünürse yüksek değer kazanır.
    /// </summary>
    public int SurfacePriority
    {
        get => (int)GetValue(SurfacePriorityProperty);
        set => SetValue(SurfacePriorityProperty, value);
    }

    public string StatusText => (string)GetValue(StatusTextProperty);
    public bool IsStreamConnected => (bool)GetValue(IsStreamConnectedProperty);

    public void Restart() => _session?.Restart();

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ScheduleForegroundWindowSync();
        _session?.ScheduleSurfaceUpdate();
    }

    internal string EffectiveSessionKey =>
        string.IsNullOrWhiteSpace(SessionKey) ? _privateSessionKey : SessionKey.Trim();

    internal void ApplySessionState(bool connected, string status)
    {
        SetValue(IsStreamConnectedPropertyKey, connected);
        SetValue(StatusTextPropertyKey, status);
        UpdateVideoStatusBridge(connected, status);
    }

    /// <summary>Video yüzeyinin durum metnini ortak <see cref="ConnectionStatus"/> diline çevirir.</summary>
    private void UpdateVideoStatusBridge(bool connected, string status)
    {
        if (VideoStatus is not { } vs) return;
        var s = status ?? string.Empty;
        if (connected)
            vs.MarkOnline("CANLI");
        else if (s.Contains("YENİDEN") || s.Contains("KESİLDİ"))
            vs.MarkRetrying(1, 2, s);
        else if (s.Contains("BAĞLANIYOR") || s.Contains("YÜKLENİYOR"))
            vs.MarkConnecting(s);
        else if (s.Contains("GEÇERLİ") || s.Contains("BAŞLATILAMADI"))
            vs.MarkFaulted(s);
        else
            vs.MarkOffline(s);
    }

    internal bool TryGetSurfaceHandle(out IntPtr handle)
    {
        ApplyTemplate();
        if (Template?.FindName(PlayerHostPartName, this) is HwndHost host
            && host.Handle != IntPtr.Zero)
        {
            handle = host.Handle;
            return true;
        }

        handle = IntPtr.Zero;
        return false;
    }

    private static object CoerceNetworkCaching(DependencyObject d, object value)
        => Math.Clamp((int)value, 0, 5000);

    private static void OnPlaybackOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RtspVideoView view)
            view._session?.UpdateOptions(view);
    }

    private static void OnSessionKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RtspVideoView view && view.IsLoaded && !view._disposed)
            view.ReattachSession();
    }

    private static void OnSurfacePriorityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RtspVideoView view)
            view._session?.ScheduleSurfaceUpdate();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_disposed)
            AttachSession();
        ScheduleForegroundWindowSync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SyncForegroundWindowVisibility();
        DetachSession();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ScheduleForegroundWindowSync();
        _session?.ScheduleSurfaceUpdate();
    }

    private void ScheduleForegroundWindowSync()
    {
        if (_disposed) return;
        Dispatcher.BeginInvoke(
            new Action(SyncForegroundWindowVisibility),
            DispatcherPriority.ContextIdle);
    }

    private void SyncForegroundWindowVisibility()
    {
        if (ForegroundWindowProperty?.GetValue(this) is not Window foregroundWindow)
            return;

        foregroundWindow.ShowActivated = false;
        var shouldShow = IsLoaded && IsVisible && ActualWidth > 0 && ActualHeight > 0;
        if (shouldShow)
        {
            if (!foregroundWindow.IsVisible)
                foregroundWindow.Show();
        }
        else if (foregroundWindow.IsVisible)
        {
            foregroundWindow.Hide();
        }
    }

    private void ReattachSession()
    {
        DetachSession();
        AttachSession();
    }

    private void AttachSession()
    {
        if (_session is not null) return;
        _session = SharedRtspSession.Acquire(this);
    }

    private void DetachSession()
    {
        var session = _session;
        _session = null;
        session?.Release(this);
        ApplySessionState(false, "AKIŞ DURDURULDU");
    }

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        IsVisibleChanged -= OnIsVisibleChanged;
        DetachSession();
        base.Dispose();
    }

    /// <summary>
    /// Decoder ve ağ bağlantısını yüzeylerden bağımsız tutar. VideoView yalnızca
    /// aktif HWND hedefidir; hedef değişirken MediaPlayer oynamaya devam eder.
    /// </summary>
    private sealed class SharedRtspSession : IDisposable
    {
        private static readonly Dictionary<string, SharedRtspSession> Sessions =
            new(StringComparer.Ordinal);

        private readonly string _key;
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherTimer _retryTimer;
        private readonly List<RtspVideoView> _views = [];

        private LibVLC? _libVlc;
        private MediaPlayer? _player;
        private Media? _media;
        private RtspVideoView? _activeView;
        private IntPtr _activeHandle;
        private string _streamUrl = string.Empty;
        private int _networkCachingMs = 150;
        private bool _useTcp = true;
        private bool _manualStop;
        private bool _playbackStarted;
        private bool _surfaceUpdateScheduled;
        private bool _disposed;
        private bool _connected;
        private string _status = "AKIŞ BEKLENİYOR";

        private SharedRtspSession(string key, Dispatcher dispatcher)
        {
            _key = key;
            _dispatcher = dispatcher;
            _retryTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _retryTimer.Tick += OnRetryTimerTick;
        }

        public static SharedRtspSession Acquire(RtspVideoView view)
        {
            var key = view.EffectiveSessionKey;
            if (!Sessions.TryGetValue(key, out var session))
            {
                session = new SharedRtspSession(key, view.Dispatcher);
                Sessions.Add(key, session);
            }

            session.Add(view);
            return session;
        }

        public void Release(RtspVideoView view)
        {
            _views.Remove(view);
            if (ReferenceEquals(_activeView, view))
            {
                if (_player is not null)
                    _player.Hwnd = IntPtr.Zero;
                _activeView = null;
                _activeHandle = IntPtr.Zero;
            }

            if (_views.Count == 0)
            {
                Sessions.Remove(_key);
                Dispose();
                return;
            }

            ScheduleSurfaceUpdate();
        }

        public void UpdateOptions(RtspVideoView source)
        {
            if (_disposed) return;

            var url = source.StreamUrl?.Trim() ?? string.Empty;
            var changed = !string.Equals(_streamUrl, url, StringComparison.Ordinal)
                          || _networkCachingMs != source.NetworkCachingMs
                          || _useTcp != source.UseTcp;

            _streamUrl = url;
            _networkCachingMs = source.NetworkCachingMs;
            _useTcp = source.UseTcp;

            if (changed || !_playbackStarted)
                PlayCurrentUrl();
        }

        public void Restart()
        {
            if (!_disposed)
                PlayCurrentUrl();
        }

        public void ScheduleSurfaceUpdate()
        {
            if (_disposed || _surfaceUpdateScheduled) return;
            _surfaceUpdateScheduled = true;
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _surfaceUpdateScheduled = false;
                UpdateActiveSurface();
            }), DispatcherPriority.Loaded);
        }

        private void Add(RtspVideoView view)
        {
            if (_views.Contains(view)) return;
            _views.Add(view);
            view.ApplySessionState(_connected, _status);
            UpdateOptions(view);
            ScheduleSurfaceUpdate();
        }

        private void EnsurePlayer()
        {
            if (_player is not null) return;

            LibVLCSharp.Shared.Core.Initialize();
            _libVlc = new LibVLC("--no-audio", "--no-video-title-show");
            _player = new MediaPlayer(_libVlc)
            {
                EnableKeyInput = false,
                EnableMouseInput = false
            };
            _player.Opening += OnOpening;
            _player.Playing += OnPlaying;
            _player.Buffering += OnBuffering;
            _player.EncounteredError += OnPlaybackFailed;
            _player.EndReached += OnPlaybackFailed;
        }

        private void PlayCurrentUrl()
        {
            EnsurePlayer();
            _retryTimer.Stop();
            _manualStop = false;
            _playbackStarted = false;

            if (!TryCreateStreamUri(_streamUrl, out var uri))
            {
                SetConnectionState(false, "GEÇERLİ RTSP URL GİRİN");
                return;
            }

            _player!.Stop();
            _media?.Dispose();
            _media = new Media(_libVlc!, uri);
            _media.AddOption($":network-caching={_networkCachingMs}");
            _media.AddOption($":live-caching={_networkCachingMs}");
            _media.AddOption(":no-audio");
            if (_useTcp)
                _media.AddOption(":rtsp-tcp");

            SetConnectionState(false, "RTSP BAĞLANIYOR");
            // Play'den ÖNCE hedef HWND'yi panele bağla; aksi halde LibVLC kendi ayrı
            // penceresini açar (video panele gömülmez). Yüzey hazırsa Hwnd hemen atanır.
            UpdateActiveSurface();
            _playbackStarted = _player.Play(_media);
            if (!_playbackStarted)
                ScheduleRetry("RTSP BAŞLATILAMADI");
        }

        private void UpdateActiveSurface()
        {
            var next = _views
                .Where(view => view.IsLoaded && view.IsVisible)
                .OrderByDescending(view => view.SurfacePriority)
                .FirstOrDefault();

            if (next is null)
            {
                if (_player is not null && _activeHandle != IntPtr.Zero)
                    _player.Hwnd = IntPtr.Zero;
                _activeView = null;
                _activeHandle = IntPtr.Zero;
                return;
            }

            if (!next.TryGetSurfaceHandle(out var nextHandle))
            {
                // Template/HWND henüz hazır değilse bir sonraki render turunda dene.
                _dispatcher.BeginInvoke(
                    new Action(ScheduleSurfaceUpdate),
                    DispatcherPriority.Render);
                return;
            }

            if (ReferenceEquals(next, _activeView) && nextHandle == _activeHandle)
                return;

            // Win32 "static" video hostu ilk kare gelene kadar beyaz boyanabilir.
            // Hedefi VLC'ye vermeden önce siyaha boyayarak beyaz parlamayı engelle.
            NativeSurfacePainter.PaintBlack(nextHandle);
            if (_player is not null)
                _player.Hwnd = nextHandle;

            _activeView = next;
            _activeHandle = nextHandle;
        }

        private static bool TryCreateStreamUri(string? value, out Uri uri)
        {
            if (Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var parsed)
                && parsed.Scheme is "rtsp" or "rtsps")
            {
                uri = parsed;
                return true;
            }

            uri = null!;
            return false;
        }

        private void OnOpening(object? sender, EventArgs e)
            => Dispatch(() => SetConnectionState(false, "RTSP BAĞLANIYOR"));

        private void OnPlaying(object? sender, EventArgs e)
            => Dispatch(() =>
            {
                _retryTimer.Stop();
                _playbackStarted = true;
                SetConnectionState(true, "CANLI");
            });

        private void OnBuffering(object? sender, MediaPlayerBufferingEventArgs e)
            => Dispatch(() =>
            {
                if (!_connected)
                    SetConnectionState(false, $"YÜKLENİYOR %{e.Cache:0}");
            });

        private void OnPlaybackFailed(object? sender, EventArgs e)
            => Dispatch(() =>
            {
                _playbackStarted = false;
                if (!_manualStop)
                    ScheduleRetry("AKIŞ KESİLDİ · YENİDEN BAĞLANIYOR");
            });

        private void ScheduleRetry(string status)
        {
            SetConnectionState(false, status);
            if (!_disposed && _views.Count > 0)
                _retryTimer.Start();
        }

        private void OnRetryTimerTick(object? sender, EventArgs e)
        {
            _retryTimer.Stop();
            Restart();
        }

        private void SetConnectionState(bool connected, string status)
        {
            _connected = connected;
            _status = status;
            foreach (var view in _views.ToArray())
                view.ApplySessionState(connected, status);
        }

        private void Dispatch(Action action)
        {
            if (_dispatcher.CheckAccess()) action();
            else _dispatcher.BeginInvoke(action);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _retryTimer.Stop();
            _retryTimer.Tick -= OnRetryTimerTick;
            _manualStop = true;
            _playbackStarted = false;

            if (_activeView is not null)
            {
                if (_player is not null)
                    _player.Hwnd = IntPtr.Zero;
                _activeView = null;
                _activeHandle = IntPtr.Zero;
            }

            _media?.Dispose();
            _media = null;
            if (_player is not null)
            {
                _player.Opening -= OnOpening;
                _player.Playing -= OnPlaying;
                _player.Buffering -= OnBuffering;
                _player.EncounteredError -= OnPlaybackFailed;
                _player.EndReached -= OnPlaybackFailed;
                _player.Stop();
                _player.Dispose();
                _player = null;
            }

            _libVlc?.Dispose();
            _libVlc = null;
        }
    }

    private static class NativeSurfacePainter
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out Rect rect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("user32.dll")]
        private static extern int FillRect(IntPtr hDc, [In] ref Rect rect, IntPtr brush);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(uint colorRef);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public static void PaintBlack(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !GetClientRect(hWnd, out var rect)) return;

            var hDc = GetDC(hWnd);
            if (hDc == IntPtr.Zero) return;

            var blackBrush = CreateSolidBrush(0x00000000);
            try
            {
                if (blackBrush != IntPtr.Zero)
                    FillRect(hDc, ref rect, blackBrush);
            }
            finally
            {
                if (blackBrush != IntPtr.Zero)
                    DeleteObject(blackBrush);
                ReleaseDC(hWnd, hDc);
            }
        }
    }
}
