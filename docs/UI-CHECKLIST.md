# GÖKDOĞAN YKİ — UI Tamamlama Checklist

> Mockup: `gokdogan_gcs_mockup.jsx` · Plan: `~/.claude/plans/o-zmana-bi-plan-giggly-sparkle.md`
> Başlangıç: 2026-04-25 · Hedef: TEKNOFEST 2026 yarışması

Bu checklist mockup'a göre WPF kod tabanını tamamlama planının ilerlemesini takip eder.
Her madde tamamlandığında `[ ]` → `[x]` olarak güncellenir. Faz bittiğinde başlık altına "✅ FAZ TAMAMLANDI · YYYY-MM-DD" notu eklenir.

> **Plan dışı (roadmap'te kalır):** LibVLCSharp gerçek RTSP video, MAVLink adapter, hedef skor motoru, video kayıt+FTP.

---

## Faz 0 — Veri Modeli Foundation ✅ TAMAMLANDI · 2026-04-25

- [x] `Core/Models/FlightMode.cs` enum (MANUAL/FBWA/AUTO/GUIDED/LOITER/RTL/STABILIZE)
- [x] `Core/Models/GpsFix.cs` enum (None/NoFix/Fix2D/Fix3D/DGPS/Rtk)
- [x] `FlightState.Speed` → `GroundSpeed` rename
- [x] `FlightState.Airspeed` eklendi
- [x] `FlightState.VerticalSpeed` eklendi
- [x] `FlightState.Mode` (FlightMode) eklendi
- [x] `FlightState.IsArmed` eklendi
- [x] `FlightState.GpsFix` (GpsFix) eklendi
- [x] `FlightState.SatelliteCount` eklendi
- [x] `FlightState.WpDistance` eklendi
- [x] `FlightState.TargetTeamNumber` (int?) eklendi
- [x] `FlightState.SignalRssi` eklendi
- [x] `IFlightCommandSink`: `Arm/Disarm/SetMode/GotoWaypoint` eklendi
- [x] `NullFlightCommandSink` yeni metodları (Debug + AlertBus)
- [x] `SimulatedFlightSource` yeni alanları besliyor
- [x] `MainWindowViewModel` yeni binding property'leri + FlightState bridge
- [x] Tests yeşil (19/19, 0 hata)

## Faz 1 — Şartname Kritikleri ✅ TAMAMLANDI · 2026-04-25

- [x] `Core/Services/Time/ServerClock.cs` (1 Hz sync, midpoint round-trip, monotonic offset)
- [x] App.xaml.cs'te ServerClock instance'ı + start (packet pump'a da bağlı)
- [x] MainWindowViewModel canlı ServerTime tick (DispatcherTimer 100ms)
- [x] TopBar canlı sunucu saati binding (ms hassas, "dd-HH:mm:ss.fff")
- [x] `Core/Services/Autonomy/KilitlenmeDenetim.cs` motoru (5 sn pencere, 4 sn valid, 5 kural)
- [x] `Core/Models/LockFrameResult.cs` + `BoundingBox` struct
- [x] `Core/Models/LockState.cs` enum (Idle/Tracking/Locking/Locked/Failed)
- [x] MainWindowViewModel: LockState/LockProgressSeconds/LockTargetId/LastLockAck binding
- [x] App.xaml.cs'te KilitlenmeDenetim instance'ı + LockSucceeded → KilitlenmeBilgisiGonder
- [x] VideoPanel kilit dörtgeni (#FF0000 stroke 2px, 640x480 native + Viewbox)
- [x] VideoPanel + CameraFullscreen crosshair (mevcut korundu)
- [x] CameraFullscreen sağ üst sunucu saati overlay (ms hassas)
- [x] "KİLİTLENME 2.7s / 4.0s" chip overlay (her iki view'da)
- [x] Tests: KilitlenmeDenetim 6 yeni test (5 kural + farklı hedef)
- [N/A] `Converters/BboxToRectConverter.cs` — VM'de hesaplı property'lerle çözüldü

> **🔍 İnceleme noktası:** Faz 0+1 agent kontrolü → bir sonraki adım.

## Faz 2 — Sağ Panel Kolonu İskeleti ✅ TAMAMLANDI · 2026-04-25

- [x] MainWindow boyut: 1920x1080, min 1600x900
- [x] Outer Grid'e sağ kolon (360px)
- [x] `Core/CollapsiblePanelFrame.xaml` + `.cs` (▼/▶ chevron + tıklanabilir header + opsiyonel badge)
- [x] App.xaml'da yeni resource dictionary kayıtlı
- [x] `Controls/Sidebar/RightSidebar.xaml` iskeleti — 8 panel iskeleti yerinde (1 dolu, 7 placeholder Faz 3 için)
- [x] AlertConsole PanelFrame kaldırıldı, sağ kolonun altında sabit (180px max)
- [x] TelemetryPanel PanelFrame kaldırıldı, sağ kolonun üstünde
- [x] MainWindow'dan TelemetryPanel + AlertConsole overlay'leri kaldırıldı
- [x] VideoPanel harita kolonu içinde kaldı (sağ kolon ile çakışmaz)
- [x] FullscreenOverlay'ler ColumnSpan=2 ile tüm window'u kaplar

## Faz 3 — Sağ Kolon Panelleri

- [ ] TelemetryPanel zenginleştirildi (V/S, AS, WP, BAT, GPS, OTONOM)
- [ ] `Sidebar/Panels/OpponentsPanel.xaml` + tablo + tıklanabilir
- [ ] `Sidebar/Panels/LockOnPanel.xaml` + progress + paket gönder butonu
- [ ] `Sidebar/Panels/KamikazePanel.xaml` UI iskeleti (FSM Faz 7'de)
- [ ] `Sidebar/Panels/ServerCommPanel.xaml` + TelemetryHzMeter
- [ ] `Core/Services/Polling/TelemetryHzMeter.cs`
- [ ] `Sidebar/Panels/SafetyPanel.xaml` + ManualTransitionCounter
- [ ] `Core/Services/Safety/ManualTransitionCounter.cs`
- [ ] HSS/Latency monitor durumlarını expose et (public observable)
- [ ] `Sidebar/Panels/CommandsPanel.xaml` UI (backend Faz 6'da)

> **🔍 İnceleme noktası:** Faz 2+3 bittikten sonra agent kontrolü.

## Faz 4 — Harita Geliştirmeleri

- [ ] `MapViewModel.OwnTrail` + polyline render
- [ ] Settings'te trail toggle + nokta sayısı slider
- [ ] `Core/Models/JammingZone.cs` + `MapViewModel.JammingZones`
- [ ] `Map/Markers/JammingCircleMarker.xaml`
- [ ] `Core/Models/Waypoint.cs` + `MapViewModel.Waypoints`
- [ ] `Map/Markers/WaypointMarker.xaml`
- [ ] Waypoint birleştirme line
- [ ] Alan çizme modu (sol tık ekle, sağ tık tamamla)
- [ ] UserPolygon kaydet/temizle butonu

## Faz 5 — HUD / PFD

- [ ] `Controls/Flight/PrimaryFlightDisplay.xaml`
- [ ] Pitch ladder + roll indicator
- [ ] Speed/Altitude tape
- [ ] Heading compass strip
- [ ] Yerleşim: kamera overlay sol-alt

> **🔍 İnceleme noktası:** Faz 4+5 bittikten sonra agent kontrolü.

## Faz 6 — Komut Backend Wire-up

- [ ] CommandsPanel butonları → MainWindowViewModel RelayCommand
- [ ] MainWindowViewModel → App.Commands.Xxx çağırır
- [ ] NullFlightCommandSink yeni metodlar (Debug + AlertBus info)
- [ ] Mod butonları toggle group + visual state
- [ ] RadialMenu vs CommandsPanel kararı (kullanıcıya sor)

## Faz 7 — Kamikaze FSM

- [ ] `Core/Services/Autonomy/KamikazeFsm.cs`
- [ ] State enum (Idle/Intikal/Dalış/QRArıyor/QROkundu/PasGeç/Tamam/Hata)
- [ ] State transition guard'lar
- [ ] KamikazePanel state binding + faz göstergesi
- [ ] Settings'e mock QR butonu (debug)
- [ ] Tests: state transition tablosu (15+ case)

> **🔍 İnceleme noktası:** Faz 6+7 bittikten sonra agent kontrolü.

## Faz 8 — Polish

- [ ] AlertToastHost (sağ üst 4s popup)
- [ ] Settings persistence (%AppData%\GOKDOGAN\settings.json + DPAPI şifre)
- [ ] Auto-reconnect loop
- [ ] TopBar status pills canlı binding
- [ ] Microsoft.Extensions.Logging entegrasyonu
- [ ] FormFieldRow refactor
- [ ] Polygon kaydet/yükle
- [ ] Mock kamikaze QR debug butonu (Settings)

> **🔍 İnceleme noktası:** Faz 8 final agent kontrolü.
