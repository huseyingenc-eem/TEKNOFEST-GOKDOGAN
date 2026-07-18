using System;
using System.Threading.Tasks;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Polling;
using GOKDOGANIHA.Core.Models.Connection;

namespace GOKDOGANIHA.Core.Services.Session;

/// <summary>
/// Login + poll servislerinin başlatılmasını tek bir iş akışına toplar. UI'nın tek
/// "connect" butonu veya App startup bunu çağırır. Login başarılı olursa
/// TelemetryPoll ve HssPoll başlar. Başarısızlık durumunda alert publish edilir.
/// TelemetryOptions.AutoReconnect etkinse oturum kaybında artan gecikmeli yeniden
/// giriş uygulanır; kullanıcının yaptığı manuel kesme bu döngüyü durdurur.
/// </summary>
public sealed class ConnectionOrchestrator
{
    private readonly IGameServerClient _client;
    private readonly TelemetryPollService _telemetryPoll;
    private readonly HssPollService _hssPoll;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private readonly TelemetryOptions _telemetryOptions;
    private readonly ConnectionStatus _status;
    private readonly object _reconnectGate = new();
    private bool _isReconnecting;
    private bool _everConnected;
    private volatile bool _manualDisconnect;
    private int _isConnected;

    public event EventHandler<QrKoordinat>? QrCoordinateReceived;
    public event EventHandler<bool>? ConnectionStateChanged;
    public bool IsConnected => Volatile.Read(ref _isConnected) == 1;

    /// <summary>Yarışma sunucusu bağlantısının gözlemlenebilir durumu (UI göstergesi için).</summary>
    public ConnectionStatus Status => _status;

    public ConnectionOrchestrator(
        IGameServerClient client,
        TelemetryPollService telemetryPoll,
        HssPollService hssPoll,
        IAlertPublisher publisher,
        IClock clock,
        TelemetryOptions telemetryOptions,
        ConnectionStatus? status = null)
    {
        _client = client;
        _telemetryPoll = telemetryPoll;
        _hssPoll = hssPoll;
        _publisher = publisher;
        _clock = clock;
        _telemetryOptions = telemetryOptions;
        _status = status ?? new ConnectionStatus("SUNUCU");
        _telemetryPoll.PollFailed += OnPollFailed;
    }

    /// <summary>
    /// Login çağrısı + poll servisleri başlatma. Başarılı olursa true döner.
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        _manualDisconnect = false;
        _status.MarkConnecting("Giriş yapılıyor…");
        try
        {
            var login = await _client.GirisAsync();
            _telemetryPoll.Start();
            _hssPoll.Start();
            _everConnected = true;
            SetConnected(true);
            _status.MarkOnline($"Bağlı · takım {login.TakimNumarasi}");
            await TryPublishQrCoordinateAsync().ConfigureAwait(false);

            _publisher.Publish(Alert.Create(
                kind: "session",
                level: AlertLevel.Info,
                title: "BAĞLANTI KURULDU",
                message: $"Giriş başarılı. Takım numarası: {login.TakimNumarasi}",
                timeUtc: _clock.UtcNow));
            return true;
        }
        catch (Exception ex)
        {
            SetConnected(false);
            _status.MarkFaulted(ex.Message);
            _publisher.Publish(Alert.Create(
                kind: "session",
                level: AlertLevel.Danger,
                title: "BAĞLANTI HATASI",
                message: ex.Message,
                timeUtc: _clock.UtcNow));
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _manualDisconnect = true;
        await _telemetryPoll.StopAsync();
        await _hssPoll.StopAsync();
        SetConnected(false);
        _status.MarkOffline("Bağlantı kesildi");
    }

    /// <summary>
    /// Telemetri PollFailed → AutoReconnect aktifse exponential backoff ile yeniden bağlan.
    /// Tek seferde bir reconnect denemesi (lock); sürekli fail durumunda 1s, 2s, 4s, 8s, max 30s.
    /// </summary>
    private async void OnPollFailed(object? sender, Exception ex)
    {
        SetConnected(false);
        if (!_telemetryOptions.AutoReconnect || !_everConnected || _manualDisconnect)
        {
            // Otomatik yeniden deneme devrede değil — kullanıcıya "Tekrar Dene" düşer.
            if (!_manualDisconnect)
                _status.MarkFaulted($"Bağlantı koptu: {ex.Message}");
            return;
        }
        lock (_reconnectGate) { if (_isReconnecting) return; _isReconnecting = true; }

        try
        {
            int attempt = 0;
            while (_telemetryOptions.AutoReconnect && !_manualDisconnect)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt - 1)));
                _status.MarkRetrying(attempt, delay.TotalSeconds,
                    $"Yeniden bağlanıyor · deneme {attempt} · {delay.TotalSeconds:F0} sn sonra");
                _publisher.Publish(Alert.Create(
                    kind: "session.reconnect",
                    level: AlertLevel.Warn,
                    title: "YENİDEN BAĞLANIYOR",
                    message: $"Deneme {attempt} · {delay.TotalSeconds:F0} sn sonra",
                    timeUtc: _clock.UtcNow));

                await Task.Delay(delay).ConfigureAwait(false);
                if (_manualDisconnect) return;

                try
                {
                    await _client.GirisAsync().ConfigureAwait(false);
                    SetConnected(true);
                    _status.MarkOnline($"{attempt}. denemede yeniden bağlandı");
                    await TryPublishQrCoordinateAsync().ConfigureAwait(false);
                    _publisher.Publish(Alert.Create(
                        kind: "session.reconnect",
                        level: AlertLevel.Info,
                        title: "YENİDEN BAĞLANDI",
                        message: $"{attempt}. denemede başarılı",
                        timeUtc: _clock.UtcNow));
                    return;
                }
                catch
                {
                    // bir sonraki backoff'a düş
                }
            }
        }
        finally
        {
            lock (_reconnectGate) _isReconnecting = false;
        }
    }

    private void SetConnected(bool value)
    {
        var next = value ? 1 : 0;
        if (Interlocked.Exchange(ref _isConnected, next) == next) return;
        ConnectionStateChanged?.Invoke(this, value);
    }

    private async Task TryPublishQrCoordinateAsync()
    {
        try
        {
            var qr = await _client.QrKoordinatiAsync().ConfigureAwait(false);
            QrCoordinateReceived?.Invoke(this, qr);
        }
        catch (Exception ex)
        {
            _publisher.Publish(Alert.Create(
                kind: "session.qr",
                level: AlertLevel.Warn,
                title: "QR KOORDİNATI ALINAMADI",
                message: ex.Message,
                timeUtc: _clock.UtcNow));
        }
    }
}
