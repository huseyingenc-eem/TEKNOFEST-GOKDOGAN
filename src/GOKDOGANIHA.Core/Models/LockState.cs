namespace GOKDOGANIHA.Core.Models;

/// <summary>
/// Kilitlenme state machine durumu. UI progress bar + LED bu enum'a göre çizilir.
/// </summary>
public enum LockState
{
    /// <summary>Hedef tespit edilmedi.</summary>
    Idle,
    /// <summary>Hedef takip ediliyor ama henüz kilit kriterleri sağlanmıyor.</summary>
    Tracking,
    /// <summary>Kilit kriterleri sağlanıyor, 4 sn'lik birikim devam ediyor.</summary>
    Locking,
    /// <summary>Kilit başarılı; sunucuya KilitlenmeBilgisi gönderildi.</summary>
    Locked,
    /// <summary>Pencere bitti, 4 sn doluya ulaşılamadı veya kural ihlali.</summary>
    Failed
}
