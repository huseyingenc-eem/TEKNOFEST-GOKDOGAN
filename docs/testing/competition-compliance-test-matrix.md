# Yarışma Haberleşmesi Test Matrisi

Referanslar:

- `docs/api/SIHA_Haberlesme_Dokumani_2026_7Vnfu.pdf`
- `docs/gorev/KTR_Yazilim_Iskeleti.pdf`
- `docs/operations/rf-allocation-2026.md`

## Otomatik testler

Platformdan bağımsız testler `GOKDOGANIHA.Core.Tests`, WPF akış testleri
`GOKDOGANIHA.Tests` projesindedir. Core testleri Linux/macOS/Windows üzerinde;
WPF testleri Windows üzerinde çalıştırılır.

## Son doğrulama sonucu — 16 Temmuz 2026

- Çözüm derlemesi: başarılı, 0 hata.
- Core/protokol/MAVLink testleri: **66 geçti, 0 başarısız**.
- Mock API: giriş öncesi saat `401`, giriş `200`, giriş sonrası saat `200`,
  ilk telemetri `200`, 2 Hz üstü anlık ikinci paket `400 / hata_kodu 3`.
- WPF test assembly'si derlendi. WindowsDesktop runtime macOS'ta bulunmadığı
  için WPF testlerinin çalıştırılması Windows CI/iş istasyonuna bırakıldı.
- Kalan NuGet uyarısı: GMap.NET 2.1.7 transitif olarak güvenlik uyarılı
  `System.Data.SqlClient 4.8.3` getiriyor. Harita sağlayıcısı güncellemesi veya
  bağımlılık değişimi ayrı bir bakım işi olarak ele alınmalı.

| Kimlik | Gereksinim | Test |
|---|---|---|
| API-01 | Telemetri 1–2 Hz | `TelemetryOptionsTests`, poll interval sınırı |
| API-02 | 2 Hz üstü reddedilir | Mock sunucu hız kontrolü; manuel entegrasyon senaryosu |
| API-03 | Bir alan hatalıysa paket bütünü geçersiz | `CompetitionTelemetryValidatorTests` |
| API-04 | Dikilme/yatış -90..90, yönelme 0..360 | `CompetitionTelemetryValidatorTests` |
| API-05 | Batarya 0..100, otonom/kilit 0 veya 1 | `CompetitionTelemetryValidatorTests` |
| API-06 | Kilit varsa hedef alanları sıfırdan farklı | `CompetitionTelemetryValidatorTests` |
| API-07 | GPS saati geçerli UTC alanları taşır | `CompetitionTelemetryValidatorTests`, `TelemetryPacketBuilderTests` |
| API-08 | HTTP 204 format hatasıdır | `GameServerClientTests` |
| API-09 | HTTP 400 hata gövdesi kaybolmaz | `GameServerClientTests` |
| FLOW-01 | Kaynak geçişinde önceki kaynak durur | `FlightBackendCoordinatorTests` |
| FLOW-02 | Kaynak hatasında komut hattı bloke kalır | `FlightBackendCoordinatorTests` |
| FLOW-03 | Simülasyon verisi canlı haritada kalmaz | `MapFlowTests` |
| FLOW-04 | Sıfır/geçersiz kendi konumu çizilmez | `MapFlowTests` |
| FAIL-01 | 10 saniyelik telemetri kaybı bir kez tetiklenir | `FailsafeMonitorTests` |

## Mock sunucu entegrasyon senaryoları

1. Mock sunucuyu başlat:

   ```bash
   dotnet run --project src/GOKDOGANIHA.MockServer
   ```

2. YKİ’de `http://127.0.0.1:5000` adresini ve Test/mock profilini seç.
3. Giriş yap; takım numarasının sunucu cevabından güncellendiğini doğrula.
4. Modal ile simülasyona geç.
5. Aşağıdaki senaryoları sırayla çalıştır:

   - `normal`: üç sentetik rakip, HSS yok.
   - `dense-opponents`: sekiz rakip ve HSS bölgeleri.
   - `hss-active`: HSS bölgeleri görünür.
   - `no-hss`: boş HSS listesi haritayı temizler.
   - `high-latency`: telemetri gecikme uyarısı üretir.

6. Telemetri hızını 1,0 / 1,5 / 2,0 Hz değerlerinde en az 30 saniye gözle.
7. Ayrı bir istemciyle 2 Hz üstünde paket gönder ve HTTP 400 + hata kodu 3
   alındığını doğrula.
8. Resmî profile geç; simülasyon açıkken mock veya resmî sunucuya telemetri
   gönderilmediğini doğrula.

## Donanım üzerinde zorunlu manuel testler

| Alan | Uygulama |
|---|---|
| MAVLink veri kaybı | UDP hattını kes; kaynak 3 sn sonra geçersiz, kendi işareti gizli ve gönderim kapalı olmalı |
| Yanlış system ID | Beklenen ID’den farklı araç paketi ortak durumu güncellememeli |
| GPS UTC | Paket yakalayıcı ile `gps_saati` değerini Pixhawk GPS zamanı ile karşılaştır |
| RF telemetri | 864,3 MHz merkez ve tahsis bant sınırlarını cihaz ekranı/spektrumla doğrula |
| RF video | 5255 MHz merkez ve kanal genişliğini iki uçta doğrula |
| Video | En az 15 fps, H.264/MP4 kayıt, sunucu zamanı ms ve kırmızı kilit dikdörtgeni |
| Gecikme | Video uçtan uca gecikme hedefi <200 ms; telemetri round-trip kaydedilmeli |
| Failsafe | LAND/RTL nihai politikası emniyet sorumlusu onayından sonra kontrollü saha testinde doğrulanmalı |

## Kabul kapısı

Yarışma profili ancak otomatik testler geçtiğinde, RF çadırı değerleri
nihaileştirildiğinde ve canlı MAVLink/GPS UTC donanım testleri imzalandığında
“hazır” kabul edilmelidir. Mock sunucunun çalışması canlı uçuş kabul testi yerine
geçmez.
