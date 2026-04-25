using System;
using System.Threading;
using System.Threading.Tasks;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Api;

namespace GOKDOGANIHA.Core.Services.Time;

/// <summary>
/// Sunucu saatini düzenli olarak <c>/api/sunucusaati</c>'ndan çeker, lokal sistem
/// saati ile arasındaki offset'i tutar. <see cref="Now"/> her çağrıldığında
/// senkron lokal saat + offset döner — UI binding'i 100ms tick ile bunu okur.
/// Şartname video kaydında ms-hassas sunucu saati ister; bu servis o veriyi
/// üretir.
/// </summary>
public sealed class ServerClock : IDisposable
{
    private readonly IGameServerClient _client;
    private readonly IClock _localClock;
    private readonly TimeSpan _syncInterval;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    /// <summary>Sunucu saati referansı: bu anda lokal-clock ne diyordu, sunucu ne diyordu?</summary>
    private readonly object _gate = new();
    private DateTime _lastSyncLocal;
    private DateTime _lastSyncServerUtc;
    private bool _hasSync;

    public ServerClock(IGameServerClient client, IClock localClock, TimeSpan? syncInterval = null)
    {
        _client = client;
        _localClock = localClock;
        _syncInterval = syncInterval ?? TimeSpan.FromSeconds(1);
    }

    /// <summary>Senkronize sunucu saati. Henüz sync olmadıysa lokal UTC döner.</summary>
    public DateTime Now
    {
        get
        {
            // Üç field birlikte tutarlı okunmalı — yazma anında okunan tearing'i engelle.
            DateTime serverRef, localRef; bool synced;
            lock (_gate) { synced = _hasSync; serverRef = _lastSyncServerUtc; localRef = _lastSyncLocal; }
            return synced ? serverRef + (_localClock.UtcNow - localRef) : _localClock.UtcNow;
        }
    }

    public bool IsSynchronized { get { lock (_gate) return _hasSync; } }

    public TimeSpan Offset
    {
        get
        {
            lock (_gate)
                return _hasSync ? _lastSyncServerUtc - _lastSyncLocal : TimeSpan.Zero;
        }
    }

    public void Start()
    {
        if (_loop is not null) return;
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunLoop(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        _cts.Cancel();
        try { if (_loop is not null) await _loop; }
        catch (OperationCanceledException) { }
        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    /// <summary>
    /// UI shutdown path'inde async dispose hang yapmasın diye kısa timeout'lu sync stop.
    /// HttpClient pending request'i 100sn timeout'a kadar bekleyebilir; biz 500ms'den sonra
    /// task'ı bırakıyoruz — process garanti çıksın.
    /// </summary>
    private void StopSyncWithTimeout()
    {
        if (_cts is null) return;
        try { _cts.Cancel(); } catch { }
        try { _loop?.Wait(TimeSpan.FromMilliseconds(500)); } catch { }
        try { _cts.Dispose(); } catch { }
        _cts = null;
        _loop = null;
    }

    private async Task RunLoop(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_syncInterval);
        // İlk sync hemen
        await TrySyncOnceAsync(ct).ConfigureAwait(false);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await TrySyncOnceAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* shutdown */ }
    }

    private async Task TrySyncOnceAsync(CancellationToken ct)
    {
        try
        {
            var localBefore = _localClock.UtcNow;
            var serverTime = await _client.SunucuSaatiAsync(ct).ConfigureAwait(false);
            var localAfter = _localClock.UtcNow;
            // Round-trip simetri varsayımı: gerçek sunucu zamanı isteğin ortasında çekildi.
            var midpoint = localBefore + TimeSpan.FromMilliseconds((localAfter - localBefore).TotalMilliseconds / 2);
            var serverUtc = ToUtc(serverTime, midpoint);
            lock (_gate)
            {
                _lastSyncServerUtc = serverUtc;
                _lastSyncLocal = midpoint;
                _hasSync = true;
            }
        }
        catch
        {
            // Sync başarısız — bir sonraki tick'te tekrar dene. Mevcut offset korunur.
        }
    }

    /// <summary>
    /// Server <see cref="ServerTime"/> kaydını absolute UTC'ye çevirir. Sunucu sadece
    /// gün-saat-dakika-saniye-ms verdiği için yıl/ay'ı lokal saatten alıyoruz.
    /// </summary>
    private static DateTime ToUtc(ServerTime t, DateTime referenceUtc)
    {
        var year = referenceUtc.Year;
        var month = referenceUtc.Month;
        var day = Math.Clamp(t.Gun, 1, DateTime.DaysInMonth(year, month));
        return new DateTime(year, month, day,
            Math.Clamp(t.Saat, 0, 23),
            Math.Clamp(t.Dakika, 0, 59),
            Math.Clamp(t.Saniye, 0, 59),
            Math.Clamp(t.Milisaniye, 0, 999),
            DateTimeKind.Utc);
    }

    public void Dispose() => StopSyncWithTimeout();
}
