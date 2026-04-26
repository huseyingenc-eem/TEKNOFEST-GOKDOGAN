namespace GOKDOGANIHA.Core.Models;

/// <summary>
/// Kamikaze görev fazları. Şartname 4 ana faz tanımlar; FSM bu enum üzerinde
/// state transition yapar. UI faz göstergesi (4 dot — INTIKAL/DALIŞ/QR/PASGEÇ)
/// bu enum'a göre dolar.
/// </summary>
public enum KamikazePhase
{
    /// <summary>Görev başlatılmadı veya iptal edildi.</summary>
    Idle,
    /// <summary>İntikal — hedefe yaklaşma irtifasında uçuyor (≥100 m yaklaşma).</summary>
    Intikal,
    /// <summary>Dalış — TYF taahhüdü 45° dalış açısı ile alçalıyor.</summary>
    Dalis,
    /// <summary>QR arama — kamera QR kodu tanımıyor.</summary>
    QrAriyor,
    /// <summary>QR okundu — pas geçilecek metin elde edildi.</summary>
    QrOkundu,
    /// <summary>Pas geçme — TYF 30 m pull-up irtifası.</summary>
    PasGec,
    /// <summary>Görev başarılı — sunucuya KamikazeBilgisi gönderildi.</summary>
    Tamam,
    /// <summary>Hata — max attempt aşıldı veya kritik failsafe.</summary>
    Hata
}
