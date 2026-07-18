using System;
using System.Threading.Tasks;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models.Alerts;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;
using GOKDOGANIHA.Core.Services.Polling;

namespace GOKDOGANIHA.Core.Services.Session;

/// <summary>
/// Login + poll servislerinin başlatılmasını tek bir iş akışına toplar. UI'nın tek
/// "connect" butonu veya App startup bunu çağırır. Login başarılı olursa
/// TelemetryPoll ve HssPoll başlar. Başarısızlık durumunda alert publish edilir.
/// Auto-reconnect şu an TODO — TelemetryOptions.AutoReconnect flag'i burada okunur.
/// </summary>
public sealed class ConnectionOrchestrator
{
    private readonly IGameServerClient _client;
    private readonly TelemetryPollService _telemetryPoll;
    private readonly HssPollService _hssPoll;
    private readonly IAlertPublisher _publisher;
    private readonly IClock _clock;
    private readonly TelemetryOptions _telemetryOptions;
    private readonly object _reconnectGate = new();
    private bool _isReconnecting;
    private bool _everConnected;

    public event EventHandler<QrKoordinat>? QrCoordinateReceived;

    public ConnectionOrchestrator(
        IGameServerClient client,
        TelemetryPollService telemetryPoll,
        HssPollService hssPoll,
        IAlertPublisher publisher,
        IClock clock,
        TelemetryOptions telemetryOptions)
    {
        _client = client;
        _telemetryPoll = telemetryPoll;
        _hssPoll = hssPoll;
        _publisher = publisher;
        _clock = clock;
        _telemetryOptions = telemetryOptions;
        _telemetryPoll.PollFailed += OnPollFailed;
    }

    /// <summary>
    /// Login çağrısı + poll servisleri başlatma. Başarılı olursa true döner.
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            var login = await _client.GirisAsync();
            _telemetryPoll.Start();
            _hssPoll.Start();
            _everConnected = true;
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
        await _telemetryPoll.StopAsync();
        await _hssPoll.StopAsync();
    }

    /// <summary>
    /// Telemetri PollFailed → AutoReconnect aktifse exponential backoff ile yeniden bağlan.
    /// Tek seferde bir reconnect denemesi (lock); sürekli fail durumunda 1s, 2s, 4s, 8s, max 30s.
    /// </summary>
    private async void OnPollFailed(object? sender, Exception ex)
    {
        if (!_telemetryOptions.AutoReconnect || !_everConnected) return;
        lock (_reconnectGate) { if (_isReconnecting) return; _isReconnecting = true; }

        try
        {
            int attempt = 0;
            while (_telemetryOptions.AutoReconnect)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt - 1)));
                _publisher.Publish(Alert.Create(
                    kind: "session.reconnect",
                    level: AlertLevel.Warn,
                    title: "YENİDEN BAĞLANIYOR",
                    message: $"Deneme {attempt} · {delay.TotalSeconds:F0} sn sonra",
                    timeUtc: _clock.UtcNow));

                await Task.Delay(delay).ConfigureAwait(false);

                try
                {
                    await _client.GirisAsync().ConfigureAwait(false);
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
