# TEKNOFEST 2026 Savaşan İHA Haberleşme Dokümanı (API)

> **Kaynak:** `docs/api/SIHA_Haberlesme_Dokumani_2026_7Vnfu.pdf`
> **Çıkarım:** `pdftotext -enc UTF-8`, 2026-04-28
> **Not:** Yarışma sunucusu HTTP/JSON API spesifikasyonu. Endpoint anchorları: `/api/giris`, `/api/sunucusaati`, `/api/telemetri_gonder`, `/api/kilitlenme_bilgisi`, `/api/kamikaze_bilgisi`, `/api/qr_koordinati`, `/api/hss_koordinatlari`.

---

SAVAŞAN İHA YARIŞMASI HABERLEŞME DOKÜMANI
2026

İÇİNDEKİLER
1. AMAÇ ................................................................................................................ 4 2. BAĞLANTI ......................................................................................................... 4 3. DURUM KODLARI ............................................................................................. 5 4. API ADRESLERİ ................................................................................................. 5 5. SUNUCUDA OTURUM AÇMA ........................................................................... 5 6. SUNUCU SAATİ................................................................................................. 6 7. TELEMETRİ ........................................................................................................ 6
7.1 Telemetri Paketi Verileri ....................................................................................... 7 7.2 Örnek Telemetri Verisi.......................................................................................... 8 7.3 Örnek Telemetri Cevabı ....................................................................................... 9 8. KİLİTLENME BİLGİSİNİN GÖNDERİLMESİ ...................................................... 10 8.1 Örnek Kilitlenme Verisi....................................................................................... 10 9. KAMİKAZE BİLGİSİNİN GÖNDERİLMESİ ......................................................... 10 9.1 Örnek Kamikaze Verisi ....................................................................................... 11 9.1 Örnek QR Kodu .................................................................................................. 11 10. QR KOORDİNATI ALINMASI ............................................................................ 11 10.1 Örnek QR Koordinatı Verisi .............................................................................. 11 11. HAVA SAVUNMA SİSTEMİ KOORDİNATLARININ ALINMASI .......................... 12 11.1 Örnek Hava Savunma Sistemi Koordinatları Verisi .......................................... 13
2

Şekiller
Şekil 1: Tüm Ağ Şeması................................................................................................................................4 Şekil 2: Örnek QR Kodu ............................................................................................................................. 11 Şekil 3: Örnek HSS Konumlandırması ....................................................................................................... 12
3

1. AMAÇ
Bu doküman TEKNOFEST 2026 kapsamında düzenlenecek olan Savaşan İHA yarışmasının sunucusu ile takımlar arasındaki haberleşmenin nasıl sağlanacağına dair bilgiler içermektedir.
2. BAĞLANTI
Takımlar yarışma sırasında telemetri ve kilitlenme bilgisi göndermek, sistem saatini ve diğer hava araçlarının konum bilgilerini almak için yarışma sunucusu ile haberleşmelidir.
Şekil 1: Tüm Ağ Şeması
Yarışma sırasında takımlara, yarışma sunucunun da içinde bulunduğu yerel ağa bağlanabilmeleri için bir ethernet kablosu sağlanacaktır. Her takım bu ethernet kablosu aracılığı ile yarışma ağına yalnızca tek bir ip adresi ile bağlanmalıdır. Yarışma sırasında takımlara birer ip adresi belirtilecek ve sisteme yalnızca belirtilen ip adresleri üzerinden bağlantıya izin verilecektir. Takımlar kendi lokal ağlarında yarışma ağına bu ip adresi üzerinden açılabileceklerdir. Takımların yarışma sunucusuna sağlıklı bağlanabilmesi ve paket kaybının engellenmesi için yarışma sunucusuna bağlanacak bilgisayarın güvenlik duvarlarının kapatılması gerekmektedir. Takımlara sunucuya bağlanması için verilen ip adreslerinden farklı olarak, ip çakışmalarının engellenmesi içinde her takıma kendi lokal ağlarına bağlı cihazlarda (yapay zekâ bilgisayarı, UBIQUITI, yedek bilgisayarlar, haberleşme modülleri, vs…) kullanması için belirli aralıkta ve sayıda ip adresleri verilecektir. Yarışma sunucusunun; gerçek adresi yarışma günü belirlenecek olup, http://127.0.0.25:5000 formatında bir adresi olacaktır. Sunucu ile yapılacak olan tüm haberleşmeler API mantığı ile JSON formatında olacaktır.
4

3. DURUM KODLARI
Yarışma sunucusuyla API üzerinden yapılan haberleşmelerde sonuç olarak aşağıdaki HTTP durum kodları alınabilir.
• 200: İstek başarılı • 204: Gönderilen paketin Formatı Yanlış • 400: İstek hatalı veya geçersiz. Böyle bir durumda hata kodu sayfa içeriği
olarak gönderilir. • 401: Kimliksiz erişim denemesi. Oturum açmanız gerekmektedir. • 403: Yetkisiz erişim denemesi. Yönetici yetkilerine sahip olmayan bir hesap
ile yöneticilere özel bağlantılara giriş yapılmaya çalışmaktadır. • 404: Geçersiz URL. • 500: Sunucu içi hata.
4. API ADRESLERİ
• GET /api/sunucusaati: Sunucu saatini sorgulamak için kullanılır. Sunucu saati ile ilgili ayrıntılı bilgi ve sunucu saati formatı, Sunucu Saati başlığında açıklanmıştır.
• POST /api/telemetri_gonder: Hava aracının bilgilerini anlık olarak sunucuya göndermek ve diğer takımların bilgilerini almak için kullanılır. Telemetri başlığında ayrıntılı bilgi verilmiştir.
• POST /api/kilitlenme_bilgisi: Bir rakip İHA’ya başarılı bir kilitlenmenin ardından kilitlenme bilgileri bu bağlantı üzerinden gönderilir. Kilitlenme Bilgisinin Gönderilmesi başlığında ayrıntılı bilgi verilmiştir.
• POST /api/giris: Takıma özel verilmiş olan kullanıcı adı ve şifre kullanarak oturum açma işlemi için kullanılır. Sisteme nasıl giriş yapılması gerektiği Sunucuda Oturum Açma başlığında anlatılmıştır.
• POST /api/kamikaze_bilgisi: Kamikaze görevi ardından okunan metin bu bağlantı üzerinden gönderilir. Kamikaze Bilgisinin gönderilmesi başlığında ayrıntılı bilgi verilmiştir.
• GET /api/qr_koordinati: Kamikaze görevi için kullanılacak olan QR kodunun yarışma alanındaki konumunu geri gönderir. Ayrıntılı bilgi QR Koordinatı Alınması başlığı altında verilmiştir.
• GET /api/hss_koordinatlari: Yarışmacıların kaçınması gereken Hava Savunma Sistemi koordinatlarının konumlarını gönderir. Müsabaka sırasında hakemlerin duyurusuyla açılacak olan hava savunma sistemlerinin koordinatları bu komut ile yarışma sunucusundan alınır. Ayrıntılı bilgi Hava Savunma Sistemi Koordinatlarının Alınması başlığı altında verilmiştir.
5. SUNUCUDA OTURUM AÇMA
Yarışma sırasında takımların sunucudan bilgi alıp bilgi gönderebilmeleri için öncelikli olarak oturum açmaları gerekmektedir. Oturum açma işleme yarışma başlamadan hemen önce bir
5

kere yapılmalıdır. Bağlantı kopması durumunda tekrardan oturum açılabilir. Takımlara sisteme giriş yapabilecekleri kullanıcı adı ve şifre bilgilerini yarışmadan önce verilecektir. Oturum açmadan yapılan sorgulamalar “401 Kimliksiz Erişim Denemesi” durum kodu ile cevaplanır. Giriş yapmak için aşağıda örneği verilmiş olan bilgiler /api/giris adresine post edilmelidir.
{ "kadi" : "takimkadi", "sifre" : "takimsifresi" }
Girişin başarılı olması durumda 200 OK durum kodu ile birlikte içerik olarak takım numarası alınır. Kullanıcı adı veya şifrenin geçersiz olması durumunda 400 durum kodu cevap olarak alınır.
6. SUNUCU SAATİ
Yarışma sırasında tüm verilerin eşzaman olması için gönderilen paketlere sunucu saati eklenmelidir. Sunucu saati /api/sunucusaati bağlantısından sorgulanabilir ve takımların sunucu ile haberleşecek bilgisayarlarının saatlerini belirtilen sunucu saati ile kalibre etmeleri önerilir. Alınan ve gönderilen tüm sunucu saatleri şu formattadır:
{ "gun": 14, "saat": 11, "dakika": 29, "saniye": 4, "milisaniye": 653
}
7. TELEMETRİ
Takımlar yarışma şartnamesinde belirtildiği gibi saniyede en az 1 Hz ile sunucuya İHA’nın durumunu belirten verileri göndermelidir. 2 Hz üzerinde gönderilen telemetri paketleri 400 durum kodu ile birlikte sayfa içeriği olarak 3 hata kodu ile cevaplanır. Telemetri paketinde bulunması gereken veriler ve açıklamaları telemetri paketi verileri başlığında açıklanmıştır. Örnek Telemetri Verisi başlığında örneği belirtilen JSON verisi api/telemetri_gonder adresine
6

post edilmelidir. Bu Postun cevabı olarak takımlar sunucu saati ile birlikte diğer yarışmacıların konum bilgilerini alabileceklerdir. Bu konum bilgilerinin içinde konum bilgisinin sunucu saati ile arasındaki zaman farkı da milisaniye türünden verilecektir. Paket içerisindeki verilerden herhangi biri belirtilen aralıklar dışında ise, tüm telemetri paketi hatalı sayılacaktır.
7.1 Telemetri Paketi Verileri
• takim_numarasi: Takıma hakemler tarafından verilen takım numarasıdır. • iha_enlem: Hava aracının ondalık biçimde enlem bilgisidir. • iha_boylam: Hava aracının ondalık biçimde boylam bilgisidir. • iha_irtifa: Hava aracının yere göre metre cinsinden yüksekliğidir. • iha_dikilme: Hava aracının derece cinsinden dikilme açısıdır. -90 ila +90 aralığında
olmalıdır. • iha_yonelme: Hava aracının derece cinsinden kuzeye göre yönelme açısıdır. 0 ila 360
aralığında olmalıdır. • iha_yatis: Hava aracının derece cinsinden yatış açısıdır. -90 ila +90 aralığında olmalıdır. • iha_hiz: Hava aracının metre/saniye cinsinden yer hızı. Hız verisi yön belirtmemelidir. • iha_batarya: Hava aracının bataryasının yüzde cinsinden doluluk oranıdır. • iha_otonom: Hava aracının otonom uçuş modunda olup olmadığının bilgisidir. Bu değer;
hava aracı otonom ise 1, değilse 0 olmalıdır. • iha_kilitlenme: Telemetrinin gönderildiği anda kilitlenme olup olmadığı bilgisidir. Eğer
hava aracı, diğer bir hava aracını takip etmeye çalışıyorsa bu değer 1 olmalıdır ve hedef ile ilgili aşağıdaki veriler sıfırdan farklı olmalıdır. • hedef_merkez_X: Hava aracının takip etmeye çalıştığı hedefin görüntüdeki konumunun piksel türünden yatay bileşenidir. Resmin sol üst noktası 0 olarak kabul edilir ve bu değer sağa doğru artar. • hedef_merkez_Y: Hava aracının takip etmeye çalıştığı hedefin görüntüdeki konumunun piksel türünden dikey bileşenidir. Resmin sol üst noktası 0 olarak kabul edilir ve bu değer aşağı doğru artar. • hedef_genislik: Görüntüdeki hedef alanının piksel türünden genişliğidir. • hedef_yukseklik: Görüntüdeki hedef alanının piksel türünden yüksekliğidir. • gps_saati: Hava aracından alınan GPS verisinin saat verisidir (UTC+0). Bu veri aşağıdaki örnekte göründüğü gibi olmalıdır. gps_saati doğrudan hava aracından gelen saat bilgisi olmalıdır.
7

7.2 Örnek Telemetri Verisi
{ "takim_numarasi": 1, "iha_enlem": 41.508775, "iha_boylam": 36.118335, "iha_irtifa": 38, "iha_dikilme": 7, "iha_yonelme": 210, "iha_yatis": -30, "iha_hiz": 28, "iha_batarya": 50, "iha_otonom": 1, "iha_kilitlenme": 1, "hedef_merkez_X": 300, "hedef_merkez_Y": 230, "hedef_genislik": 30, "hedef_yukseklik": 43,"gps_saati": { "saat": 11, "dakika": 38, "saniye": 37, "milisaniye": 654 }
}
8

7.3 Örnek Telemetri Cevabı
{ "sunucusaati": {"gun": 13, "saat": 11, "dakika": 38, "saniye": 38, "milisaniye": 739 }, "konumBilgileri": [ { "takim_numarasi": 1, "iha_enlem": 41.5118256, "iha_boylam": 36.11993, "iha_irtifa": 36.0, "iha_dikilme": -8.0, "iha_yonelme": 127, "iha_yatis": 19.0, "iha_hizi": 41.0, "zaman_farki": 467 }, { "takim_numarasi": 2, "iha_enlem": 41.5100365, "iha_boylam": 36.11837, "iha_irtifa": 44.0, "iha_dikilme": 24.0, "iha_yonelme": 277.0, "iha_yatis": -37.0, "iha_hizi": 40.0, "zaman_farki": 248 }, { "takim_numarasi": 3, "iha_enlem": 41.5123138, "iha_boylam": 36.12, "iha_irtifa": 32.0, "iha_dikilme": 9.0, "iha_yonelme": 13, "iha_yatis": -30.0, "iha_hizi": 45.0, "zaman_farki": 30 } ]
}
9

8. KİLİTLENME BİLGİSİNİN GÖNDERİLMESİ
Takımlar gerçekleştirdikleri her başarılı kilitlenmenin ardından sunucuya kilitlenme bilgisi göndermelidir. Kilitlenme bilgisinin içerisinde kilitlenmenin bittiği zaman ve kilitlenmenin otonom olup olmadığı bilgisi bulunmalıdır. Zaman bilgileri sunucu saati türünde olmalıdır. Eğer kilitlenme otonom yapılmışsa otonom_kilitlenme verisi 1 değerinde olmalıdır. Kilitlenme bilgisi gönderilmeden yapılan kilitlenmeler puanlandırmaya tabii tutulmaz. Kilitlenme bilgisi kilitlenmenin bitiminden sonra gönderilmelidir ve her kilitlenme için yalnızda bir paket gönderilmelidir. Gönderilen kilitlenme bilgileri, yarışma oturumunun bitiminde kaydedilen videolar ve yeniden oynatma sistemleri kullanılarak incelenecek puanlandırılacaktır. Kilitlenme verisi örneği, Örnek Kilitlenme Verisi başlığında gösterilmiştir.
8.1 Örnek Kilitlenme Verisi
{ "kilitlenmeBitisZamani": {"saat": 11, "dakika": 41, "saniye": 03, "milisaniye": 141 }, "otonom_kilitlenme": 1
}
9. KAMİKAZE BİLGİSİNİN GÖNDERİLMESİ
Takımlar gerçekleştirdikleri başarılı kamikaze görevi ardından sunucuya kamikaze bilgisi göndermelidir. Kamikaze bilgisinin içinde kamikaze başlangıç zamanı, kamikaze bitiş zamanı ve QR kodun okunması sonucu elde edilen metin bilgisi olmalıdır. Zaman bilgileri sunucu saati türünde olmalıdır. Kamikaze bilgisi kamikaze bitiminden sonra gönderilmelidir ve her kamikaze için yalnızda bir paket gönderilmelidir. Gönderilen kamikaze bilgileri, yarışma oturumunun bitiminde kaydedilen videolar ve yeniden oynatma sistemleri kullanılarak incelenecektir. Kamikaze verisi örneği, Örnek Kamikaze Verisi başlığında gösterilmiştir. QR kod örneği Örnek QR Kodu başlığında gösterilmiştir. Yarışmada kullanılacak QR kodu Versiyon 1 olacaktır.
10

9.1 Örnek Kamikaze Verisi
{ "kamikazeBaslangicZamani" : {"saat": 11, "dakika": 44, "saniye": 13, "milisaniye": 361 }, "kamikazeBitisZamani": {"saat": 11, "dakika": 44, "saniye": 27, "milisaniye": 874 }, "qrMetni ": “teknofest2025”
}
9.1 Örnek QR Kodu
Şekil 2: Örnek QR Kodu
10. QR KOORDİNATI ALINMASI
Takımlar sunucuya gönderecekleri sorgu ile müsabakada kullanılacak olan QR kodunun konumunu alabilmektedir. Bu bilgi içinde QR kodunun konumunun enlem ve boylam bilgileri bulunmaktadır. QR koordinatı örneği Örnek QR koordinatı Verisi başlığı altında verilmiştir.
10.1 Örnek QR Koordinatı Verisi
{ "qrEnlem": 41.51238882, "qrBoylam": 36.11935778
}
11

11. HAVA SAVUNMA SİSTEMİ KOORDİNATLARININ ALINMASI
Takımlar sunucuya gönderecekleri sorgu ile müsabakada aktif olacak Hava Savunma Sistemlerinin koordinatlarını alabilmektedir. Bu bilgi içinde sunucu saati, HSS’lerin ID, enlem, boylam ve yarıçap bilgileri bulunmaktadır. ID, hava savunma sisteminin numarasını temsil eder. HSS’ler arasında karışıklık olmaması adına her bir HSS’ye bir ID atanmıştır. Enlem, boylam ve yarıçap bilgileri takımların kaçınması gereken çemberleri temsil eder. Merkezi ilgili enlem ve boylamlar olan ilgili yarıçaplardaki dairelerden kaçınılması gerekmektedir. Hava Savunma Sistemi koordinatları, yalnızca hakemler duyuru yaptığında sunucudan çekilebilmektedir. Hakemlerin Hava Savunma Sistemi’nin aktif olması ile ilgili duyuru yapmadığı zamanlarda sunucuya gönderilecek sorgulara cevap olarak boş liste dönecektir. Hakemlerin duyurusu ardından sunucudan çekilmeye hazır olacak HSS bilgisi örneği Örnek Hava Savunma Sistemi Koordinatları Verisi başlığı altında verilmiştir.
Şekil 3: Örnek HSS Konumlandırması
Görsel temsilidir. Uçuş alanı yarışma öncesinde duyurulacaktır.
12

11.1 Örnek Hava Savunma Sistemi Koordinatları Verisi
{ "sunucusaati": { "gun": 19, "saat": 15, "dakika": 51, "saniye": 43, "milisaniye": 775 }, "hss_koordinat_bilgileri": [ { "id": 0, "hssEnlem": 40.23260922, "hssBoylam": 29.00573015, "hssYaricap": 50 }, { "id": 1, "hssEnlem": 40.23351019, "hssBoylam": 28.99976492, "hssYaricap": 50 }, { "id": 2, "hssEnlem": 40.23105297, "hssBoylam": 29.00744677, "hssYaricap": 75 }, { "id": 3, "hssEnlem": 40.23090554, "hssBoylam": 29.00221109, "hssYaricap": 150 } ]
}
13

14

