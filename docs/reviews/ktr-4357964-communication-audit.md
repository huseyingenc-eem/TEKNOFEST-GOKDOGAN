# 4357964.pdf - Haberleşme Teknik Tutarlılık Notları

Bu notlar KTR'nin 3.3, 6 ve 8.1.3 bölümleri ile 2026 şartname/haberleşme
dokümanı ve mevcut yazılım karşılaştırılarak hazırlanmıştır.

## Doğru ve kodla uyumlu noktalar

- Cube Orange+ - RFD868x - YKİ hattında MAVLink kullanılması.
- YKİ - yarışma sunucusu hattının doğrudan kablolu Ethernet olması.
- MAVLink telemetrisinin JSON'a dönüştürülerek 1-2 Hz HTTP POST ile
  gönderilmesi ve rakip konumlarının cevap içinden alınması.
- RFD868x haberleşme testinin 2,5 km, -65 dBm ve %1 altı paket kaybı sonucu.
- Sunucu bağlantısının ayarlar ekranından yapılması ve yan panelden izlenmesi.

## KTR'de düzeltilmesi gereken ifadeler

1. **6.1'deki "10 MHz kanal" ifadesi:** 5,1-5,9 GHz arasında 10 MHz kanal
   ayarlanabilirliği video/Wi-Fi cihazıyla ilgilidir. RFD868x için 868-869 MHz bandı,
   hakemlerin tahsis ettiği frekans ve Net ID anlatılmalı; 10 MHz ifadesi telemetri
   paragrafından çıkarılmalıdır.
2. **"Şifreli MAVLink" ifadesi:** MAVLink kendi başına şifreli değildir. RFD868x
   AES şifreleme gerçekten etkinleştirilip test edildiyse modem katmanında AES
   kullanıldığı yazılmalı; aksi halde "MAVLink paketleri" denmelidir.
3. **3.3.2.3'te PowerBeam'in sunucu hattı gibi anlatılması:** PowerBeam yer video
   alıcısıdır. Yarışma sunucusu bağlantısı hakem Ethernet anahtarı üzerinden
   ayrı kablolu hattır. Bu paragraf 6. bölümle çelişmektedir.
4. **Yazılım kütüphaneleri:** Raporda ReactiveUI, Emgu.CV ve LiveChartsCore
   kullanıldığı yazıyor; mevcut proje bağımlılıklarında bunlar yoktur. Raporda
   yalnızca gerçekten kullanılan CommunityToolkit.Mvvm, GMap.NET, LibVLCSharp ve
   uygulamanın kendi MAVLink ayrıştırıcısı belirtilmelidir.
5. **Seri bağlantı ayrıntısı eksik:** Yer RFD868x'in YKİ bilgisayarına USB/COM
   olarak bağlandığı, baud değerinin iki uçla aynı olması ve hattın 8N1 olduğu
   açıklanmalıdır.
6. **Canlı komut iddiası:** KTR, YKİ'den uçağa komut gönderildiğini söylüyor.
   Mevcut emniyetli sürüm canlı telemetriyi okur ancak fiziksel MAVLink komut
   adaptörü kapalıdır. Rapor tesliminde uygulamanın gerçek kabiliyetiyle bu iddia
   aynı hale getirilmelidir.
