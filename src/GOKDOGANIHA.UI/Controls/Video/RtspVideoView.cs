using System.Windows;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

namespace GOKDOGANIHA.UI.Controls.Video;

/// <summary>
/// RTSP adresini oynatan, görünürlük değişimlerinde bağlantıyı yöneten ve
/// geçici ağ hatalarında yeniden deneyen ortak WPF video yüzeyi.
/// </summary>
public sealed class RtspVideoView : VideoView
{
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

    private readonly DispatcherTimer _retryTimer;
    private LibVLC? _libVlc;
    private MediaPlayer? _player;
    private Media? _media;
    private bool _manualStop;
    private bool _disposed;

    public RtspVideoView()
    {
        _retryTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _retryTimer.Tick += OnRetryTimerTick;
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

    public string StatusText => (string)GetValue(StatusTextProperty);
    public bool IsStreamConnected => (bool)GetValue(IsStreamConnectedProperty);

    public void Restart()
    {
        if (!IsLoaded || !IsVisible || _disposed) return;
        PlayCurrentUrl();
    }

    private static object CoerceNetworkCaching(DependencyObject d, object value)
        => Math.Clamp((int)value, 0, 5000);

    private static void OnPlaybackOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RtspVideoView view)
            view.Restart();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsurePlayer();
        Restart();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopPlayback();
        DisposePlayer();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (IsVisible) Restart();
        else StopPlayback();
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
        MediaPlayer = _player;
    }

    private void PlayCurrentUrl()
    {
        EnsurePlayer();
        _retryTimer.Stop();
        _manualStop = false;

        if (!TryCreateStreamUri(StreamUrl, out var uri))
        {
            SetConnectionState(false, "GEÇERLİ RTSP URL GİRİN");
            return;
        }

        _player!.Stop();
        _media?.Dispose();
        _media = new Media(_libVlc!, uri);
        _media.AddOption($":network-caching={NetworkCachingMs}");
        _media.AddOption($":live-caching={NetworkCachingMs}");
        _media.AddOption(":no-audio");
        if (UseTcp)
            _media.AddOption(":rtsp-tcp");

        SetConnectionState(false, "RTSP BAĞLANIYOR");
        if (!_player.Play(_media))
            ScheduleRetry("RTSP BAŞLATILAMADI");
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
            SetConnectionState(true, "CANLI");
        });

    private void OnBuffering(object? sender, MediaPlayerBufferingEventArgs e)
        => Dispatch(() =>
        {
            if (!IsStreamConnected)
                SetConnectionState(false, $"YÜKLENİYOR %{e.Cache:0}");
        });

    private void OnPlaybackFailed(object? sender, EventArgs e)
        => Dispatch(() =>
        {
            if (!_manualStop)
                ScheduleRetry("AKIŞ KESİLDİ · YENİDEN BAĞLANIYOR");
        });

    private void ScheduleRetry(string status)
    {
        SetConnectionState(false, status);
        if (IsLoaded && IsVisible && !_disposed)
            _retryTimer.Start();
    }

    private void OnRetryTimerTick(object? sender, EventArgs e)
    {
        _retryTimer.Stop();
        Restart();
    }

    private void StopPlayback()
    {
        _retryTimer.Stop();
        _manualStop = true;
        _player?.Stop();
        SetConnectionState(false, "AKIŞ DURDURULDU");
    }

    private void SetConnectionState(bool connected, string status)
    {
        SetValue(IsStreamConnectedPropertyKey, connected);
        SetValue(StatusTextPropertyKey, status);
    }

    private void Dispatch(Action action)
    {
        if (Dispatcher.CheckAccess()) action();
        else Dispatcher.BeginInvoke(action);
    }

    private void DisposePlayer()
    {
        _media?.Dispose();
        _media = null;
        if (_player is not null)
        {
            _player.Opening -= OnOpening;
            _player.Playing -= OnPlaying;
            _player.Buffering -= OnBuffering;
            _player.EncounteredError -= OnPlaybackFailed;
            _player.EndReached -= OnPlaybackFailed;
            _player.Dispose();
            _player = null;
        }
        _libVlc?.Dispose();
        _libVlc = null;
        MediaPlayer = null;
    }

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        IsVisibleChanged -= OnIsVisibleChanged;
        _retryTimer.Stop();
        _retryTimer.Tick -= OnRetryTimerTick;
        StopPlayback();
        DisposePlayer();
        base.Dispose();
    }
}
