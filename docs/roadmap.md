# GÖKDOĞAN YKİ — Kalan İşler (Roadmap)

Bu doküman, `main` branch'inde şu anki state'e göre **henüz yapılmamış büyük işleri** listeler.
Her madde bir sonraki iterasyon adayıdır; öncelik sırası kritiklik + yarışma akışına katkıya göredir.

> Son güncelleme: 2026-04-23 · Son merge: `2b4abb6 Merge: proximity monitors + Connect UI + IFlightStateSource`
> Test durumu: **19/19 yeşil**.

---

## 🔴 Yarışma akışı için kritik

### 1. LibVLCSharp gerçek RTSP akışı
**Problem:** `VideoPanel` ve `CameraFullscreenView` video surface olarak sadece placeholder TextBlock gösteriyor. Rakip takımı görmek için gerçek video stream gerekli.

**Kapsam:**
- NuGet: `LibVLCSharp.WPF 3.8.x` + `VideoLAN.LibVLC.Windows` (~40 MB)
- `App.xaml.cs`'te `Core.Initialize()` startup call'u
- `VideoPanel.xaml` → `VideoView` (LibVLCSharp) + placeholder'ı failsafe olarak tut
- `VideoOptions.RtspUrl` değişince `MediaPlayer.Stop/Play(new Media(url))` re-init
- Connection failure handling (timeout, retry)
- Snapshot save (PNG) — `CameraFullscreenView` SNAPSHOT butonu için

**Dosyalar:** `Controls/Video/VideoPanel.xaml(.cs)`, `Controls/Video/CameraFullscreenView.xaml(.cs)`, `App.xaml.cs`, `GOKDOGANIHA.UI.csproj`

### 2. MAVLink adapter — `IFlightStateSource` + `IFlightCommandSink` gerçek impl
**Problem:** Şu an `SimulatedFlightSource` sine-wave ile FlightState'i besliyor; `NullFlightCommandSink` sadece Debug'a log atıyor. Gerçek Pixhawk'la konuşamıyor.

**Kapsam:**
- NuGet: `MAVLink` (AlexanderPoe/C# port) veya `MAVSDK`
- `Services/Mavlink/MavlinkFlightStateSource.cs` — UDP listener (ArduPilot default `udp://0.0.0.0:14550`)
  - `GLOBAL_POSITION_INT` → `FlightState.Latitude/Longitude/Altitude`
  - `ATTITUDE` → `FlightState.Roll/Pitch/Heading`
  - `SYS_STATUS` + `BATTERY_STATUS` → `FlightState.BatteryPercent/BatteryVoltage`
  - `HEARTBEAT` → `IsArmed`, mode
- `Services/Mavlink/MavlinkFlightCommandSink.cs` — komut gönderici
  - `Rtl()` → `MAV_CMD_NAV_RETURN_TO_LAUNCH`
  - `Land()` → `MAV_CMD_NAV_LAND`
  - `Loiter()` → `MAV_CMD_NAV_LOITER_UNLIM`
- `App.xaml.cs`: Configuration flag ile `SimulatedFlightSource` ↔ `MavlinkFlightStateSource` arasında seç
- Ayarlar sekmesi: MAVLINK tab (bağlantı tipi UDP/Serial/TCP, port, baud, system ID)
- ArduPilot SITL ile test (Docker veya WSL)

**Dosyalar:** `src/GOKDOGANIHA.Core/Services/Mavlink/`, `Configuration/MavlinkOptions.cs`, `App.xaml.cs`

### 3. Hedef seçim skor motoru (`IHedefSkorMotoru`)
**Problem:** `AutonomyOptions` içinde skor ağırlıkları (w_mesafe/w_açı/w_geçmiş/w_risk) tanımlı ama kimse kullanmıyor. KTR dokümanı "puan kurtarıcı" olarak açıkça bu skorlama sistemini talep ediyor.

**Kapsam:**
- `Core/Services/Autonomy/HedefSkorMotoru.cs`
  - `Score(FlightState self, KonumBilgisi opponent, AutonomyOptions opts, IReadOnlyList<int> lockHistory, IReadOnlyList<HssKoordinat> activeHss) → double`
  - P_mesafe = `(d_max − d_i) / d_max` (normalize 0..1)
  - P_açı = `1 − |Δψ| / 180` (burun yönü uyumu)
  - P_geçmiş = `lockHistory.Contains(target.Id) ? 0 : 1` (art-arda kilit yasağı)
  - P_risk = HSS ile mesafeye göre monotonic decay
  - Toplam skor = w1·P_mesafe + w2·P_açı + w3·P_geçmiş − w4·P_risk
- `Core/Services/Autonomy/TargetSelector.cs` — `TelemetryPoll.TelemetryReceived`'a subscribe; en yüksek skorlu rakibi seçip `FlightState.TargetTeamNumber` (yeni property) olarak yaz
- Unit test: ağırlıkları değiştirince farklı hedef seçiliyor mu

**Dosyalar:** `Core/Services/Autonomy/`, `FlightState.cs` (target id ekle)

### 4. Kilitlenme denetim motoru (`IKilitlenmeDenetim`)
**Problem:** Şartname kilitlenmeye ait 5 kural tanımlıyor (4s sliding window + %5 tolerans + %90 kapsama + tekrar-kilit yasağı + merkez kontrol). Şu an hiçbiri yok; `IsLocked` basit bir boolean.

**Kapsam:**
- `Core/Services/Autonomy/KilitlenmeDenetim.cs`
  - Her frame (25-30 Hz): `EvaluateFrame(bbox_hedef, bbox_kilitlenme, hedef_id)` → `LockFrameResult`
  - 5 saniye sliding window (FIFO queue)
  - Kriterler:
    1. Hedef merkez kilitlenme dikdörtgeni içinde (yatay/dikey toleranslar)
    2. `|bbox_hedef.W - bbox_kilitlenme.W| / bbox_hedef.W < LockTolerancePercent/100`
    3. `IoU(bbox_hedef, bbox_kilitlenme) ≥ 0.9`
    4. Son 5 sn'de toplam 4 sn valid → `LockSucceeded` event
    5. `last_locked_id != current_target.id` (tekrar-kilit yasağı)
- Lock succeeded → `IGameServerClient.KilitlenmeBilgisiGonderAsync()`
- Unit test: sentetik frame serisi ile her kural ayrı ayrı doğrulanır

**Dosyalar:** `Core/Services/Autonomy/KilitlenmeDenetim.cs`, `Models/LockFrameResult.cs`

### 5. Kamikaze FSM (`IKamikazeGorevi`)
**Problem:** Şartname 4 fazlı kamikaze (intikal → dalış → QR okuma → pas geç) + TYF taahhüdü (100m/45°/30m) öngörüyor. `AutonomyOptions` parametreleri var ama state machine yok.

**Kapsam:**
- `Core/Services/Autonomy/KamikazeFsm.cs` — state enum (Idle/Intikal/Dalış/QR/PasGeç/Tamam/Hata)
- Her state için guard + transition conditions (irtifa eşikleri, QR tespit, max 2 attempt)
- QR okuma: OpenCV/Pyzbar wrapper (yeni NuGet `Emgu.CV` zaten var)
- Success → `IGameServerClient.KamikazeBilgisiGonderAsync(qrMetni)`
- Pull-up: `IFlightCommandSink.SetPitch(+30°)` stub (MAVLink adapter'da gerçek impl)
- UI: Kamera overlay'de faz göstergesi (INTIKAL / DALIŞ / QR / PAS GEÇ)

**Dosyalar:** `Core/Services/Autonomy/KamikazeFsm.cs`, `Controls/Video/CameraFullscreenView.xaml`

---

## 🟠 Önemli ama bloker değil

### 6. AlertToastHost — geçici popup uyarıları
**Problem:** AlertConsole logging yapıyor ama kullanıcı ekran başında değilken kritik alert'i kaçırabilir. Eski React prototipte sağ üstte 3-4 sn göstergesi vardı.

**Kapsam:**
- `Controls/Alerts/AlertToastHost.xaml(.cs)` — `ItemsControl` + CollectionAnimation
- `MainWindow.xaml` sağ üst köşe (TopBar altı) ekle
- `AlertBus.AlertPublished` → Toast queue'ya ekle, 4 sn sonra otomatik sil
- Level bazlı renk + slide-in animation

**Dosyalar:** `Controls/Alerts/AlertToastHost.xaml(.cs)`, `Views/MainWindow.xaml`

### 7. Ayar persistence — `%AppData%\GOKDOGAN\settings.json`
**Problem:** Uygulamayı kapat-aç → ayarlar sıfırlanıyor (yalnızca in-memory Options). Yarışma öncesi takım numarası + eşikleri her seferinde tekrar girmek gerek.

**Kapsam:**
- `Core/Services/Persistence/SettingsStore.cs`
  - `Load(string path) → ApplicationOptions`
  - `Save(ApplicationOptions, string path)`
  - `System.Text.Json` + `JsonSerializerOptions { WriteIndented = true }`
- `App.xaml.cs OnStartup`: load at boot, merge with defaults
- `ApplicationOptions.PropertyChanged` → debounced save (1 sn)
- Şifre alanı: plain JSON → DPAPI encrypt (Windows Data Protection API)
- Unit test: round-trip serialize + default fallback

**Dosyalar:** `Core/Services/Persistence/`

### 8. Login retry loop — `TelemetryOptions.AutoReconnect`
**Problem:** `AutoReconnect` property var ama tüketicisi yok. Network kopunca bağlantı ölü kalır.

**Kapsam:**
- `ConnectionOrchestrator`: `PollFailed` event'i → exponential backoff retry
- `TelemetryPoll.PollFailed` sürekli failing ise → session alert + dur
- UI: TopBar'da bağlantı durumu indicator (kırmızı/sarı/yeşil nokta)

**Dosyalar:** `Core/Services/Session/ConnectionOrchestrator.cs`, `Controls/Shell/TopBar.xaml`

---

## 🟢 Polish / Nice to have

### 9. Mission planner (Waypoint editor)
Eski React prototipteki MissionPanel/MissionScreen kavramı. Harita'ya tıklayıp waypoint ekleme, route editing, İHA'ya gönderme. MAVLink `MISSION_ITEM_INT` mesajları gerek (#2'ye bağımlı).

### 10. Contact list panel (rakip İHA listesi)
`TelemetryResponse.KonumBilgileri`'ni tablo olarak göster — hangi takım ne kadar yakın, hız/yön. Tıklayınca haritada highlight. Eski React `ContactList.jsx`'in WPF port'u.

### 11. PFD widget (Primary Flight Display)
Eski React `PFD.jsx`'teki suni ufuk + hız şeridi + altitude tape. `FlightState` property'lerine bind. Yeni `Controls/Flight/PrimaryFlightDisplay.xaml`.

### 12. Status pill live updates
TopBar'daki LINK/API/BAT pill'leri şu an hardcoded değerleri gösteriyor. FlightState + TelemetryPoll metrics ile canlı bağlamak (RSSI, API latency, batarya yüzdesi).

### 13. Logger altyapısı
`Debug.WriteLine` + `MessageBox.Show` yerine structured logging (`Microsoft.Extensions.Logging` + dosya + console). Yarışma sonrası incident analysis için kritik.

### 14. Video kayıt + FTP upload (şartname zorunluluğu)
Sunucu saati ms hassas overlay + kilitlenme dörtgeni #FF0000 + H.264/MP4 + FTP yükleme — şartnamede zorunlu. LibVLCSharp `MediaPlayer.TakeSnapshot` + kayıt API'si veya ayrı FFmpeg wrapper. #1'e bağımlı.

### 15. Tests — integration + end-to-end
- Mock yarışma sunucusu (`ASP.NET Core TestServer` veya simple `HttpListener`) — full telemetri akışı testi
- UI smoke test (WPF UI automation — karmaşık, opsiyonel)

### 16. Form refactor — `FormFieldRow` UserControl
`SettingsView.xaml`'da 13+ Grid boilerplate kalıyor. Faz 5'te ertelenmişti. Reusable `<local:FormFieldRow Label="..." Value="{Binding ...}" />` component'i + XAML 200+ satır kısalır.

---

## Faz dışı notlar

### Geofence algoritması — basit dist-to-edge ötesi
`BoundaryProximityMonitor` şu an basit "en yakın kenara mesafe" kullanıyor. TYF şartnamesi poligon içi/dışı durumuna göre farklı aksiyon isteyebilir (içerideysen "sınıra X m kaldı" uyarısı; dışarıdaysan "RTL" zorunlu). Point-in-polygon test + winding number gerekebilir.

### HSS APF algoritması — otonom kaçınma
KTR dokümanı Artificial Potential Field kullanımını "puan kurtarıcı" olarak işaret ediyor. `Services/Autonomy/ApfHssAvoidance.cs` — çekici (hedef) + itici (HSS) kuvvetler, yerel minimum kontrolü. `IFlightCommandSink.SetHeading(degrees)` ile yaw komutu.

### Kalman filter + IoU tracker
Kamera görüntü işleme tarafı (OpenCV): hedef takip için Kalman predict + Hungarian assignment. Bu iş yazılım ekibinin YOLO entegrasyonunu yaptıktan sonra gelecek.

---

## Nasıl güncellenmeli?
Her yeni iterasyonda:
1. İlgili maddeyi **DONE** olarak işaretle veya sil
2. Yeni gelen iş isteklerini doğru bölüme ekle
3. Son güncelleme tarihini ve son merge'i yukarıda güncelle
