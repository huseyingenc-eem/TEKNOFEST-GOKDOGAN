# KTR Bölüm 6 — Çıkartılamaz Bilgiler

> Şartname (V1.1, 11.04.2026) ve KTR Genel Şablonu'na göre KTR'nin 6. bölümünden **kelimesi kelimesine çıkarılamayacak** bilgilerin listesi. Kısaltma yapılırken bu maddelerin hepsi yazıda kalmak zorundadır.

---

## 1. Şartname m. 6.1.2 — Haberleşme

- Üç haberleşme cihazının **frekans aralıkları**: 868–869 MHz (telemetri), 5.15–5.875 GHz (görüntü/Wi-Fi), 2.4 GHz (RC kontrol)
- **Frekans atama mantığı**: Net ID özelleştirilebilirliği, 5.1–5.9 GHz aralığında 10 MHz genişlikte kanal ayarlanabilirliği
- Cihazların şartnamede istenen ayarlanabilirlik şartını sağladığının açıkça gösterilmesi

## 2. Şartname m. 6.1.3 — Yarışma Sunucusu

- YKİ ↔ Sunucu bağlantısının **ethernet ağ anahtarı üzerinden kablolu** olması (internet/router/kablosuz değil)
- Telemetri gönderim hızı: **saniyede 1 ila 2 paket** (min 1 Hz, max 2 Hz)
- Gönderilen her veri paketine **sunucudan alınan milisaniye hassasiyetli sistem saati** eklenmesi

## 3. Şartname m. 6.1.4 — Telemetri Bilgisi

- Telemetri verilerinin **doğrudan otopilot bilgisayarı tarafından üretilmesi**
- Üzerinde **interpolasyon, ekstrapolasyon veya kopyalama** işlemi uygulanmaması

## 4. Şartname m. 7.1.2 — KTR Genel

- Donanımsal aygıtların **nicelikleri ve nitelikleri** (model isimleri + sayısal özellikler)
  - **Cube Orange+** (otopilot)
  - **NVIDIA Jetson Xavier NX** (görev bilgisayarı)
  - **Arducam Ar234** (kamera)
  - **RFD868x** (telemetri modemi, 40 km menzil)
  - **Ubiquiti Rocket M5** (Wi-Fi modülü, 300 Mbps)
  - **Ubiquiti PowerBeam PBE-M5-400** (yönlü anten, 25 dBi)
  - **Radiolink AT10II** (RC kumanda)
  - **R9DS** (RC alıcı)
- Yazılımsal aygıtların nicelikleri ve nitelikleri (C# WPF, .NET 10)

## 5. KTR Şablonu m. 6 — Haberleşme Açıklaması

Aşağıdakilerin **ayrıntılı şekilde açıklanması ve diyagram olarak gösterilmesi**:

- Anten frekansları
- Haberleşme protokolleri (**MAVLink**, **S-BUS**, **HTTP/JSON**, **UDP**)
- Haberleşme donanımları
- Görüntü aktarım sistemi
- Takipçi anten durumu (kullanılıp kullanılmadığı, gerekçesi)
- Yarışma sunucusu ile haberleşme
- **Diyagram (Görsel 6.1)** — sistem mimarisi şeması

## 6. KTR Şablonu m. 6 — Arayüz Tasarımı

Arayüzde **görsellerle gösterilmesi gereken** öğeler:

- Hız
- Yükseklik
- Mod değişimi (uçuş modu)
- Kilitlenme dörtgeni
- Hava savunma sistemleri (HSS)
- Uçuş sınırları (yarışma alanı)
- Rakip hava araçları (rakip İHA'lar)
- **Arayüz görseli (Görsel 6.3)** — gerçek ekran görüntüsü

---

## Notlar

- **Esnek olan kısımlar** (çıkarılabilir ama içerik fakirleşir): 6.5'in detayları (MVVM, kütüphane isimleri, async/await), 6.6'daki uyarı listesi, 6.7'nin tamamı, anten yerleşim hesabı açıklaması, UDP/TCP tercih gerekçesi.
- **Cihaz seçim gerekçesi** ("neden RFD868x") şartname/şablon tarafından zorunlu kılınmıyor; yazmak iyi-niyet puanı sağlar ama çıkarılabilir.
- **Görsel 6.1 ve Görsel 6.3** placeholder olamaz; KTR teslimine kadar gerçek diyagram ve screenshot konulmalıdır.
