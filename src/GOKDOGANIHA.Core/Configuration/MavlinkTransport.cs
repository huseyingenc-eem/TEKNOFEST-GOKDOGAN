namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Canlı MAVLink akışının YKİ'ye hangi fiziksel/lojik hat üzerinden ulaştığını belirtir.
/// RFD868x USB modem doğrudan kullanılıyorsa <see cref="Serial"/>; SITL, MAVProxy
/// veya bir ağ köprüsü kullanılıyorsa <see cref="Udp"/> seçilir.
/// </summary>
public enum MavlinkTransport
{
    Udp,
    Serial
}
