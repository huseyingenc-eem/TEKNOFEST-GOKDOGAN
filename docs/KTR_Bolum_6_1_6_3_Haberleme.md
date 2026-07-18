# 6. Yer Kontrol İstasyonu, Haberleşme ve Kullanıcı Arayüzü

Savaşan İHA görevlerinde otonom uçuş performansının ve hedef tespit algoritmalarının sorunsuz çalışabilmesi için haberleşme altyapısının kesintisiz, düşük gecikmeli ve senkronize olması gerekmektedir. Bu doğrultuda hava aracı (İHA) ile Yer Kontrol İstasyonu (YKİ) arasındaki veri akışı; telemetri, yüksek hızlı Wi-Fi modülü ve RC kontrolcü olmak üzere üç ana haberleşme katmanına ayrılmıştır. YKİ ile yarışma sunucusu arasındaki bağlantı ise bu üç hattan tamamen bağımsız olarak müsabaka alanındaki ethernet ağ anahtarı üzerinden kablolu olarak tesis edilir. İlgili haberleşme mimarisi Görsel 6.1 üzerinde şematize edilmiştir.

> *Görsel 6.1 — Üç katmanlı haberleşme mimarisi (yer tutucu — eklenecek).*

## 6.1. Telemetri Sistemi ve Uçuş Verisi Aktarımı

Uçuş anında otopilot (Cube Orange+) verilerinin yer istasyonuna iletilmesi ve YKİ'den otopilota otonom görev komutlarının aktarılması için RFD868x telemetri modülleri kullanılmaktadır. Sistem 868–869 MHz aralığında çalışmakta, görüş hattında 40 km'ye kadar menzilde veri iletimi sağlamaktadır. Veri paketlemesi MAVLink protokolü ile yapılmakta; modül üzerinden frekans aralığı ve Net ID değerleri yazılımsal olarak özelleştirilebilmekte ve takım bu sayede hakem heyetinin atayacağı kanala uyum sağlayabilmektedir. Sunucuya iletilen telemetri verileri doğrudan otopilot bilgisayarı tarafından üretilir; üzerinde herhangi bir interpolasyon, ekstrapolasyon veya kopyalama işlemi uygulanmaz.

## 6.2. Görüntü ve Hedef Verisi Aktarımı

İHA üzerindeki Arducam Ar234 kameradan alınan anlık görüntüler ve NVIDIA Jetson Xavier NX görev bilgisayarında işlenen yapay zeka çıkarımları (hedef tespit verileri) yüksek bant genişliğine ihtiyaç duyar. Bu nedenle görüntü ve yapay zeka verilerinin aktarımı için 5.15–5.875 GHz aralığında çalışan Wi-Fi tabanlı bir sistem kurulmuştur:

- **Hava Aracı:** Ubiquiti Rocket M5
- **Yer İstasyonu:** Ubiquiti PowerBeam PBE-M5-400

Yer istasyonunda kullanılan PowerBeam PBE-M5-400, 25 dBi kazançlı yönlü bir anten olduğundan yarışma uçuş hacminin tamamına yakınını kapsayacak optimum bir açı ve yükseklikte sabit olarak konumlandırılacaktır. Antenin ışıma açısı (beamwidth) ile yarışma alanının sınırları göz önünde bulundurularak yapılan hesaplamalar sayesinde otomatik bir takipçi anten (tracker) ihtiyacı ortadan kaldırılmıştır. Bu çift ile 20 km menzile kadar kararlı bir görüntü aktarımı sağlanabilmekte; görüntü aktarımında TCP'nin paket onay gecikmelerini yaşamamak adına UDP protokolü tercih edilerek 300 Mbps'ye varan teorik bağlantı hızlarında gecikmesiz bir video akışı elde edilmektedir. Cihazlar 5.1–5.9 GHz aralığında 10 MHz genişliğinde kanala atanabilecek niteliktedir; takıma bildirilen frekans aralığı yarışma süresince sabit kalacak şekilde yapılandırılır.

## 6.3. RC Kumanda ve Alıcı Haberleşmesi

İHA temel olarak otonom uçuş yapsa da, güvenlik tedbirleri ve acil durum müdahaleleri için Radiolink AT10II kumanda ve R9DS alıcı kullanılmaktadır. Bu haberleşme 2.4 GHz frekansında gerçekleşmekte olup, alıcı ile uçuş kontrolcüsü arasındaki veri iletimi S-BUS protokolü üzerinden sağlanmaktadır. Üç haberleşme katmanının bağımsız frekans ve protokol kullanması, herhangi birinin kesilmesi durumunda diğerlerinin görevi sürdürebilmesini sağlayarak sistem üzerindeki tek nokta arıza riskini ortadan kaldırır.

## 6.4. Yarışma Sunucusu Protokolü

YKİ ile yarışma sunucusu arasındaki tüm haberleşme HTTP üzerinden JSON formatında gerçekleştirilmektedir. Yarışma başlamadan önce takıma özel kullanıcı adı ve şifre ile /api/giris adresine oturum açma isteği gönderilir; oturum açılmadan diğer uç noktalara yapılan sorgular kimliksiz erişim olarak reddedilir. Oturum sağlandıktan sonra YKİ; sunucu saatini /api/sunucusaati üzerinden senkronize eder, otopilot tarafından üretilen telemetri paketlerini saniyede 1 ila 2 paket aralığında /api/telemetri_gonder uç noktasına iletir ve aynı yanıtın içinden rakip İHA'ların 1 Hz konum bilgisini alır. Gönderilen her veri paketine sunucudan alınan milisaniye hassasiyetli sistem saati eklenmektedir. Başarılı bir kilitlenme tamamlandığında /api/kilitlenme_bilgisi, kamikaze görevinde okunan QR sonucu ise /api/kamikaze_bilgisi uç noktasına gönderilir. Kamikaze hedefinin yarışma alanındaki konumu /api/qr_koordinati üzerinden, hakemler tarafından duyurulduğunda aktive edilen hava savunma sistemi koordinatları ise /api/hss_koordinatlari üzerinden alınır.

## 6.5. Yer Kontrol İstasyonu Yazılımı

Yer Kontrol İstasyonu yazılımı, .NET 10 üzerinde C# ve WPF kullanılarak özgün olarak geliştirilmektedir; takımın C# tecrübesi ve WPF'in yüksek performanslı arayüz altyapısı bu yığının tercih sebebidir. Yazılım MVVM tasarım deseni üzerine inşa edilmiş, arayüz ve donanım servisleri birbirinden bağımsız modüllere ayrılmıştır. MVVM altyapısı CommunityToolkit.Mvvm ve ReactiveUI; harita gösterimi GMap.NET; görüntü işleme Emgu.CV ve telemetri grafikleri LiveChartsCore kütüphaneleriyle sağlanmaktadır. MAVLink ayrıştırması için bir MAVLink kütüphanesi, sunucu iletişimi için .NET'in HttpClient sınıfı kullanılmaktadır. Tüm I/O süreçleri async/await ile asenkron işlenerek arayüzün kilit yememesi sağlanmıştır. Yazılım mimarisi Görsel 6.2'de gösterilmiştir.

> *Görsel 6.2 — Yer Kontrol İstasyonu yazılım mimarisi (yer tutucu — eklenecek).*

## 6.6. Kullanıcı Arayüzü Tasarımı

Arayüz; harita, video ve uçuş bilgi paneli olmak üzere üç ana bölgeden oluşur. Harita üzerinde yarışma alanı sınırları, hava aracımız, rakip İHA'lar ve aktif hava savunma sistemleri canlı olarak gösterilmekte; video akışına kilitlenme dörtgeni overlay olarak çizilmektedir. Uçuş bilgi panelinde hız, yükseklik, batarya, uçuş modu ve telemetri gecikmesi yer alır. Sınır ve HSS yaklaşması, batarya kritik seviyesi ile başarılı kilitlenme durumları için renk kodlu uyarılar tetiklenmektedir. Sunucu bağlantısı ayarlar penceresinden yapılmakta, durumu yan panelden takip edilmektedir. Geliştirilen arayüz Görsel 6.3'te gösterilmiştir.

> *Görsel 6.3 — Yer Kontrol İstasyonu kullanıcı arayüzü ana ekran (yer tutucu — eklenecek).*

## 6.7. Müsabaka Video Kaydı

İHA üzerindeki kameradan gelen RTSP video akışı yer istasyonunda H.264 sıkıştırma ile MP4 formatında diske kaydedilmektedir. Kayıt sabit kare hızında yapılandırılmış olup saniyede 15 karenin altına düşmesine izin verilmemektedir. Görüntünün sağ üst köşesinde milisaniye hassasiyetli sunucu saati yazılmakta, kilitlenme süresince hedef hava aracını çevreleyen dikdörtgen #FF0000 renginde her kareye çizilmektedir. Kayıt formatı OpenCV 4.5 ve FFPLAY ile sorunsuz açılabilecek şekilde seçilmiş; müsabaka sonrası 10 dakika içinde FTP sunucusuna yüklenmektedir.
