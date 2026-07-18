namespace GOKDOGANIHA.Core.Models.Connection;

/// <summary>
/// Bir bağlantının (yarışma sunucusu, telemetri, video) yaşam döngüsü durumu.
/// Üç bağlantı da aynı dili kullanır; böylece UI tutarlı renk/ikon/metin gösterir
/// ve tek bir yeniden kullanılabilir gösterge hepsine hizmet eder.
/// </summary>
public enum ConnectionPhase
{
    /// <summary>Hiç bağlanılmadı veya kullanıcı bilerek kesti. Nötr (gri).</summary>
    Offline,

    /// <summary>İlk bağlanma denemesi sürüyor. Meşgul (sarı).</summary>
    Connecting,

    /// <summary>Bağlı ve veri akıyor. Sağlıklı (yeşil).</summary>
    Online,

    /// <summary>Bağlantı koptu; otomatik yeniden bağlanma sürüyor. Uyarı (sarı, sayaçlı).</summary>
    Retrying,

    /// <summary>Bağlanılamadı ve otomatik deneme yok/tükendi. Hata (kırmızı).</summary>
    Faulted
}
