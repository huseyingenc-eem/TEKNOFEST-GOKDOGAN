# 2026 Geçici RF Frekans Ataması

Kaynak: Hakem kurulunun takıma gönderdiği frekans atama e-postası. Atamalar
yarışma günü RF çadırında değiştirilebilir ve nihaileştirilir.

| Hat | Kanal başı | Merkez | Kanal sonu | Uygulamadaki anlamı |
|---|---:|---:|---:|---|
| 5 GHz görüntü | 5250,0 MHz | 5255,0 MHz | 5260,0 MHz | Video RF taşıyıcı/kanal tahsisi |
| Telemetri | 864,0 MHz | 864,3 MHz | 864,5 MHz | RFD telemetri RF taşıyıcı/kanal tahsisi |

Bu değerler `/api/telemetri_gonder` hız ayarı değildir. HTTP telemetri gönderim
hızı resmî haberleşme dokümanına göre **1–2 Hz** olarak kalmalıdır.

## Donanım uyumluluk değerlendirmesi

- KTR iskeletinde Rocket M5 + PowerBeam M5 için 5,15–5,875 GHz çalışma aralığı
  yazıyor. Atanan 5255 MHz merkez bu beyan edilen aralığın içindedir.
- KTR iskeletinde RFD868x için 868–869 MHz yazıyor. Atanan 864,3 MHz merkez bu
  beyanın dışındadır. Bu, doğrudan “cihaz kesin desteklemiyor” anlamına gelmez;
  fakat kullanılan RFD868x donanım revizyonu, bölgesel firmware ve kanal
  bant genişliği üzerinden hemen doğrulanmalıdır.
- RFD cihaz arayüzünde 864,3 MHz merkez veya 864,0–864,5 MHz tahsisinin
  yapılandırılamadığı görülürse, e-postadaki talimata uygun şekilde yalnızca
  desteklenmeyen cihaz için TEKNOFEST iletişime dönüş yapılmalıdır.

## RF çadırı ve sistem tanımlama videosu kontrol listesi

- [ ] Video link cihazında merkez frekans 5255,0 MHz olarak gösterildi.
- [ ] Seçilen kanal genişliğinin 5250,0–5260,0 MHz tahsisine uyduğu doğrulandı.
- [ ] Telemetri cihazında merkez frekans 864,3 MHz olarak gösterildi.
- [ ] Telemetri kanal genişliği/hava veri hızı 864,0–864,5 MHz tahsisine sığıyor.
- [ ] İHA ve yer istasyonu tarafında aynı frekans, ağ kimliği ve modem ayarları var.
- [ ] Spektrum analizörü veya cihaz spektrum ekranında tahsis dışı yayın görülmedi.
- [ ] Frekans ayarı yapılmayan diğer cihazların çalışma aralıkları videoda belirtildi.
- [ ] Ayar ekranları ve cihaz model/firmware bilgileri sistem tanımlama videosuna alındı.
- [ ] Yarışma günü RF çadırındaki nihai değerler bu belgeye işlendi.

## Yazılım akışına etkisi

RF değerleri YKİ içindeki telemetri JSON’una eklenmez. YKİ yalnızca link sağlığı,
MAVLink paket güncelliği ve mümkünse modem RSSI bilgisini izler. Radyo frekansını
değiştirecek üretici API/CLI entegrasyonu bulunmadığı sürece uygulama üzerinden
“frekans ayarlandı” kabul edilmemelidir; bu kontrol fiziksel cihaz arayüzünde
yapılır.
