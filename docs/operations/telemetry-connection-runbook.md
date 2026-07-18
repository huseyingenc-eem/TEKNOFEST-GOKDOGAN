# GÖKDOĞAN Telemetri ve Yarışma Sunucusu Bağlantı Kılavuzu

Bu kılavuz 2026 Savaşan İHA şartnamesi, resmî haberleşme dokümanı ve
`4357964.pdf` KTR'sindeki RFD868x/Cube Orange+ mimarisi esas alınarak hazırlanmıştır.

## 1. Bağlantı mimarisi

Bağlantı iki bağımsız hattan oluşur:

1. **İHA -> YKİ canlı uçuş verisi:** Cube Orange+ `TELEM` portu -> hava
   RFD868x -> 868-869 MHz RF hattı -> yer RFD868x -> YKİ bilgisayarı USB/COM ->
   MAVLink.
2. **YKİ -> yarışma sunucusu:** Hakemlerin verdiği Ethernet kablosu ve tek IP ->
   HTTP/JSON API. Bu hat RFD868x veya Ubiquiti video hattından geçmez.

## 2. RFD868x ve MAVLink bağlantısı

### Donanım ön kontrolü

- Hava ve yer RFD868x modüllerinin Net ID, hava veri hızı, seri hız ve tahsis
  edilen RF frekans ayarları birbiriyle aynı olmalıdır.
- RF frekansı ve Net ID yarışma görevlilerinin bildirdiği değerlere göre cihaz
  konfigürasyon aracından ayarlanmalıdır. Uygulama RF modem parametrelerini değil,
  modemin bilgisayara sunduğu MAVLink seri akışını yapılandırır.
- Cube Orange+ ve RFD868x arasındaki UART kablosunda TX/RX çapraz, GND ortak
  olmalıdır. Gerilim/pin dizilimi cihaz üreticisinin dokümanından doğrulanmalıdır.
- Yer RFD868x USB ile takıldıktan sonra Windows Aygıt Yöneticisi'ndeki COM portu
  not edilmelidir.

### Uygulama ayarları

1. Ana ekranda sağ üstteki **Ayarlar** simgesini açın.
2. **Bağlantı -> Telemetri -> MAVLink Canlı Bağlantı** bölümüne gidin.
3. `Bağlantı türü = Serial` seçin.
4. RFD868x'in COM portunu seçin. Liste güncel değilse **Yenile** kullanın.
5. RFD868x ve Cube `SERIALx_BAUD` ayarıyla aynı baud değerini girin. Uygulama
   varsayılanı `57600`, hat formatı `8N1`'dir; gerçek donanım ayarı farklıysa
   uygulamadaki değer mutlaka ona eşitlenmelidir.
6. `Beklenen system ID` alanına aracın MAVLink System ID'sini yazın. `0`, ilk
   geçerli HEARTBEAT'i gönderen sistemi otomatik seçer ve diğer sistemleri reddeder.
7. **MAVLink'i Yeni Ayarlarla Bağla** butonuna basın.
8. Durum `CANLI` olana kadar bekleyin. Sistem geçerli HEARTBEAT almadan canlı
   sayılmaz. Resmî sunucuya veri gönderimi için ayrıca 3D/DGPS/RTK fix, geçerli
   araç konumu ve otopilottan gelen GPS UTC zamanı gerekir.

Seri kablo çıkarılırsa otomatik yeniden bağlanma aynı COM portunu iki saniyede
bir yeniden açmayı dener. Windows yeniden takılan cihaza farklı COM numarası verirse
ayar yeni portla güncellenmelidir.

### SITL veya MAVProxy

SITL/MAVProxy kullanılıyorsa `Bağlantı türü = Udp`, dinleme adresi `0.0.0.0`
ve varsayılan port `14550` kullanılabilir. UDP modu fiziksel RFD868x'in USB/COM
bağlantısının yerine geçmez; arada seri-UDP köprüsü varsa kullanılır.

## 3. Yarışma sunucusu bağlantısı

1. Hakemlerin verdiği Ethernet kablosunu sunucuya bağlanacak YKİ bilgisayarına
   takın.
2. Yalnızca hakemlerin takıma atadığı IP, alt ağ maskesi ve gerekiyorsa ağ geçidi
   değerlerini Windows Ethernet adaptörüne girin. Yarışma ağına tek IP ile
   çıkılmalıdır.
3. Resmî haberleşme dokümanı güvenlik duvarının kapatılmasını ister. Bu işlem
   yalnızca yarışma ağı profili için ve hakem/BT sorumlusu gözetiminde yapılmalı;
   mümkünse tüm güvenlik duvarı yerine uygulama ve yarışma sunucusu için gerekli
   kural açılmalıdır.
4. **Ayarlar -> Bağlantı -> Yarışma Sunucusu** bölümünde yarışma günü
   verilen gerçek `http://<sunucu-ip>:5000` adresini girin. Dokümandaki
   `http://127.0.0.25:5000` yalnızca adres formatı örneğidir.
5. Takıma verilen kullanıcı adı ve şifreyi girin. Resmî bağlantıda
   **Test/mock sunucu profili** kapalı olmalıdır.
6. **Bağlantıyı Test Et** ile `/api/giris` cevabını ve sunucunun verdiği takım
   numarasını doğrulayın.
7. **Bağlan - Poll Başlat** ile oturumu ve periyodik telemetri/HSS akışını
   başlatın. Yan panelde `Bağlı`, gecikme ve 1-2 Hz telemetri durumu görülmelidir.

Uygulama `/api/giris`, `/api/sunucusaati`, `/api/telemetri_gonder`,
`/api/kilitlenme_bilgisi`, `/api/kamikaze_bilgisi`, `/api/qr_koordinati` ve
`/api/hss_koordinatlari` uç noktalarını destekler. 2 Hz üstü gönderim uygulama
ayarında engellenir; paket alanları dokümana aykırıysa paket sunucuya çıkmadan
reddedilir.

## 4. Uçuş öncesi kabul kontrolü

- [ ] RFD868x iki uçta aynı RF/Net ID/seri hız ayarlarında.
- [ ] Uygulamada doğru COM portu ve baud seçili.
- [ ] MAVLink durumu `CANLI`; doğru System ID görünüyor.
- [ ] 3D veya daha iyi GPS fix ve otopilot GPS UTC zamanı mevcut.
- [ ] Yükseklik `GLOBAL_POSITION_INT.relative_alt` kaynağından AGL olarak geliyor.
- [ ] Hakem Ethernet kablosu ve atanmış tek IP yapılandırıldı.
- [ ] Sunucu oturumu bağlı, takım numarası sunucu cevabından alındı.
- [ ] Telemetri yanıtı 1-2 Hz aralığında ve rakip konumları alınıyor.
- [ ] Kablo kesme, yanlış System ID ve 3 saniyelik veri kaybı senaryoları test edildi.
- [ ] Kilitlenme paketi olaydan sonra en geç 2 saniye içinde ulaşıyor.

## 5. Bilinçli emniyet sınırı

Canlı MAVLink okuma ve yarışma sunucusu haberleşmesi hazırdır. Fiziksel
uçağa MAVLink komutu gönderen adaptör emniyet gereği ayrı tutulmuş ve bu sürümde
kapalıdır. Arm, mod değiştirme, RTL/LAND ve waypoint komutları donanım-çevrimli
emniyet testleri ve yetkili onayı olmadan etkinleştirilmemelidir.
