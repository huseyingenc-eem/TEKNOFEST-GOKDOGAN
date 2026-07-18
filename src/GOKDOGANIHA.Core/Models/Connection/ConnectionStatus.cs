using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GOKDOGANIHA.Core.Models.Connection;

/// <summary>
/// Tek bir bağlantının gözlemlenebilir durumu. Servis katmanı (ConnectionOrchestrator,
/// MavlinkFlightStateSource, RtspVideoView köprüsü) <c>Mark*</c> metodlarıyla durumu
/// ilerletir; UI, <see cref="INotifyPropertyChanged"/> üzerinden dinler.
///
/// SOLID: SRP — yalnızca durum tutar ve bildirir. Bağlanma/retry mantığı çağıran
/// serviste kalır; bu tip UI'a ve servise bağımlı değildir, bu yüzden birim testlerle
/// tam doğrulanabilir.
/// </summary>
public sealed class ConnectionStatus : INotifyPropertyChanged
{
    private ConnectionPhase _phase = ConnectionPhase.Offline;
    private string _message = "Bağlı değil";
    private int _retryAttempt;
    private double _nextRetryInSeconds;
    private DateTime _lastChangeUtc;

    public ConnectionStatus(string name)
        => Name = string.IsNullOrWhiteSpace(name) ? "BAĞLANTI" : name.Trim();

    /// <summary>Kullanıcıya gösterilen kısa ad — "SUNUCU", "TELEMETRİ", "VİDEO".</summary>
    public string Name { get; }

    public ConnectionPhase Phase { get => _phase; private set => Set(ref _phase, value); }

    /// <summary>Duruma eşlik eden, insan-okur kısa açıklama (UI'da doğrudan gösterilir).</summary>
    public string Message { get => _message; private set => Set(ref _message, value); }

    /// <summary><see cref="ConnectionPhase.Retrying"/> fazında kaçıncı deneme (1..n); diğer fazlarda 0.</summary>
    public int RetryAttempt { get => _retryAttempt; private set => Set(ref _retryAttempt, value); }

    /// <summary>Bir sonraki denemeye kalan yaklaşık saniye; Retrying dışında 0.</summary>
    public double NextRetryInSeconds { get => _nextRetryInSeconds; private set => Set(ref _nextRetryInSeconds, value); }

    /// <summary>Son durum değişiminin UTC zamanı — "5 sn önce" gibi tazelik göstermek için.</summary>
    public DateTime LastChangeUtc { get => _lastChangeUtc; private set => Set(ref _lastChangeUtc, value); }

    // ----- Türetilmiş bayraklar (UI trigger'ları/binding'leri kolaylaştırır) -----

    public bool IsOnline => Phase == ConnectionPhase.Online;
    public bool IsBusy => Phase is ConnectionPhase.Connecting or ConnectionPhase.Retrying;
    public bool IsFaulted => Phase == ConnectionPhase.Faulted;
    public bool IsOffline => Phase == ConnectionPhase.Offline;

    // ----- Durum geçişleri (yalnızca servis katmanı çağırır) -----

    /// <summary>İlk bağlanma denemesi başladı.</summary>
    public void MarkConnecting(string? message = null)
        => Transition(ConnectionPhase.Connecting, message ?? "Bağlanıyor…", attempt: 0, nextRetryInSeconds: 0);

    /// <summary>Bağlantı kuruldu ve veri akıyor.</summary>
    public void MarkOnline(string? message = null)
        => Transition(ConnectionPhase.Online, message ?? "Bağlı", attempt: 0, nextRetryInSeconds: 0);

    /// <summary>Bağlantı koptu; otomatik yeniden deneme sürüyor. <paramref name="attempt"/> 1'e sıkıştırılır.</summary>
    public void MarkRetrying(int attempt, double nextRetryInSeconds, string? message = null)
        => Transition(
            ConnectionPhase.Retrying,
            message ?? $"Yeniden bağlanıyor · deneme {Math.Max(1, attempt)}",
            attempt: Math.Max(1, attempt),
            nextRetryInSeconds: Math.Max(0, nextRetryInSeconds));

    /// <summary>Bağlanılamadı ve otomatik deneme yok/tükendi.</summary>
    public void MarkFaulted(string message)
        => Transition(ConnectionPhase.Faulted,
            string.IsNullOrWhiteSpace(message) ? "Bağlantı hatası" : message,
            attempt: 0, nextRetryInSeconds: 0);

    /// <summary>Kullanıcı bilerek kesti veya hiç bağlanılmadı.</summary>
    public void MarkOffline(string? message = null)
        => Transition(ConnectionPhase.Offline, message ?? "Bağlı değil", attempt: 0, nextRetryInSeconds: 0);

    private void Transition(ConnectionPhase phase, string message, int attempt, double nextRetryInSeconds)
    {
        var phaseChanged = _phase != phase;
        Phase = phase;
        Message = message;
        RetryAttempt = attempt;
        NextRetryInSeconds = nextRetryInSeconds;
        LastChangeUtc = DateTime.UtcNow;

        // Türetilmiş bayraklar Phase'e bağlı; faz değişince UI'ya haber ver.
        if (phaseChanged)
        {
            OnPropertyChanged(nameof(IsOnline));
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsFaulted));
            OnPropertyChanged(nameof(IsOffline));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }

    private void OnPropertyChanged(string? name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
