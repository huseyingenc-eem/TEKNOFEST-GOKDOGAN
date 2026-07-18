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

## Faz 3 — Sağ Kolon Panelleri ✅ TAMAMLANDI · 2026-04-26

- [x] TelemetryPanel kompakt/full (HUD overlay olarak, sidebar'da değil — UX kararı)
- [x] `Sidebar/Panels/OpponentsPanel.xaml` + tablo (TAKIM/KONUM/İRT/HIZ)
- [x] `Sidebar/Panels/LockOnPanel.xaml` + LED + 4s progress + paket gönder butonu
- [x] `Sidebar/Panels/KamikazePanel.xaml` UI iskeleti (FSM Faz 7'de bağlanır)
- [x] `Sidebar/Panels/ServerCommPanel.xaml` + bağlantı LED + Hz + latency + RSSI
- [x] `Core/Services/Polling/TelemetryHzMeter.cs` (5 sn kayan pencere, INPC)
- [x] `Sidebar/Panels/SafetyPanel.xaml` + sınır/HSS/manuel/failsafe/dropout
- [x] `Core/Services/Safety/ManualTransitionCounter.cs`
- [x] BoundaryProximity/Hss/CommLatency/Failsafe monitor'ları INotifyPropertyChanged ile durum expose
- [x] `Sidebar/Panels/CommandsPanel.xaml` UI — 6 mod butonu + 6 komut (IsEnabled=false, Faz 6 backend)
- [x] App.xaml.cs HzMeter + ManualTransitions instantiate + dispose
- [x] MainWindowViewModel 100ms tick'te tüm sidebar property'lerini günceller

> **🔍 İnceleme noktası:** Faz 2+3 agent kontrolü — bir sonraki adım.

## Faz 4 — Harita Geliştirmeleri ✅ TAMAMLANDI · 2026-04-26

- [x] `MapViewModel.OwnTrail` + polyline render (max 200 nokta FIFO, ~2m noise filter)
- [x] Trail toggle (ShowOwnTrail) — Settings persistence Faz 8'de
- [x] `Core/Models/JammingZone.cs` + `MapViewModel.JammingZones`
- [x] `Map/Markers/JammingCircleMarker.cs` (mor + dashed stroke, zoom-aware)
- [x] `Core/Models/Waypoint.cs` + `MapViewModel.Waypoints`
- [x] `Map/Markers/WaypointMarker.xaml` (numaralı yuvarlak pin + glow effect)
- [x] Waypoint birleştirme line (dashed teal route)
- [x] Alan çizme modu (sol tık vertex / sağ tık tamamla / crosshair cursor)
- [x] UserPolygon temizle butonu + ÇİZİM MODU rozet overlay
- [x] Map toolbar: ÇİZİM, polygon temizle, İZ toggle, iz temizle (4 buton)
- [x] FlightState bridge fix: MainWindowViewModel artık MapVm.SetOwnPosition'ı çağırıyor (önceden boş kalıyordu)
- [x] OnMapZoomChanged: HSS + Jamming circle'ları yeniden boyutlanıyor

## Faz 5 — HUD / PFD ✅ TAMAMLANDI · 2026-04-26

- [x] `Controls/Flight/PrimaryFlightDisplay.xaml(.cs)` — 280×180 kompakt UserControl
- [x] Pitch ladder (±10°/±20° işaretleri) + roll RotateTransform (DP callback ile manual scale)
- [x] Speed tape (sol) + Altitude tape (sağ) — büyük readout + tape style
- [x] Heading compass strip (alt) + center triangle indicator
- [x] V/S sub-readout — semantik renk (yeşil=tırmanış, sarı=düşüş, kırmızı=hızlı düşüş)
- [x] Aircraft reference symbol (sabit, rotate etmez)
- [x] CameraFullscreenView'da eski 3 dikey IAS/ALT/HDG card kaldırıldı, yerine PFD
- [x] DependencyProperty'ler (Pitch/Roll/Heading/Altitude/Airspeed/VerticalSpeed) — bağımsız VM-agnostic

> **🔍 İnceleme noktası:** Faz 4+5 agent kontrolü → bir sonraki adım.

## Faz 6 — Komut Backend Wire-up ✅ TAMAMLANDI · 2026-04-26

- [x] CommandsPanel 12 butonu → MainWindowViewModel RelayCommand
- [x] MainWindowViewModel → App.Commands.Xxx çağırır (Arm/Disarm toggle, SetMode parametreli, Rtl, Land, Loiter, GotoWaypoint, ToggleAutonomous, SelectTarget)
- [x] NullFlightCommandSink AlertBus.Publish (Faz 0'da yapılmıştı, Faz 6'da kullanıldı)
- [x] Mod butonları toggle group + visual state — aktif mod yeşil highlight (StringEqualsConverter)
- [x] ARM/DISARM butonu IsArmed'a göre text değişir
- [x] Hover state mode butonlarında — TacticalAccent border
- [x] ToolTip her butonda — UX için (jüri puanı)

## Faz 7 — Kamikaze FSM ✅ TAMAMLANDI · 2026-04-26

- [x] `Core/Models/KamikazePhase.cs` enum (Idle/Intikal/Dalis/QrAriyor/QrOkundu/PasGec/Tamam/Hata)
- [x] `Core/Services/Autonomy/KamikazeFsm.cs` state machine (INPC) + KamikazeMissionResult record
- [x] State transition guard'lar (irtifa eşikleri, max 2 attempt, distance < 250m, pull-up * 1.5 trigger)
- [x] StartMission / Tick(FlightState) / ApplyQrRead / Abort API
- [x] KamikazePanel: 4 dot phase indicator + canlı durum metni + QR konum/metin/deneme + 3 buton (BAŞLAT / QR SİM / İPTAL)
- [x] Phase severity rengi (Hata kırmızı, Tamam yeşil)
- [x] App.xaml.cs: KamikazeFsm instance + MissionCompleted handler (success → KamikazeBilgisi paket gönder)
- [x] MainWindowViewModel: tick'te Tick(FlightState) çağrılır, 5 binding property
- [x] StartKamikaze / SimulateQrRead / AbortKamikaze RelayCommand
- [x] Tests: 12 state transition test (Initial/Start/4-phase progression/QR ignore/Success/Retry/MaxAttempts/Abort/Idle)

> **🔍 İnceleme noktası:** Faz 6+7 agent kontrolü → bir sonraki adım.

## Faz 7.5 — KamikazeFullscreenView ✅ TAMAMLANDI · 2026-04-26

- [x] `Controls/Kamikaze/KamikazeFullscreenView.xaml(.cs)` — full ekran taktik görünüm
- [x] APPROACH GEOMETRY (sol) — SVG Path bezier eğrisi + drone ikonu canlı (Pitch + Altitude'a göre)
- [x] OPTICAL TARGETING (sağ) — kamera placeholder + crosshair + QR detection rect (faza göre sarı/yeşil)
- [x] DISTANCE TO TARGET + VERTICAL VELOCITY büyük metric tile'lar
- [x] DECODER STATUS + QR Metni satırı
- [x] Büyük ABORT KAMİKAZE butonu (alt, kırmızı)
- [x] 4 dot phase indicator üst-sağ (Hata'da hepsi kırmızı)
- [x] MainWindow OverlayHost ile entegrasyon (IsKamikazeFullscreen, ColumnSpan=2)
- [x] Auto-open: KamikazeFsm.IsActive olunca açılır, biter bitmez kapanır
- [x] Mutual exclusion: Camera/Settings overlay'leri ile çakışmaz (öncelikli)
- [x] Sim modunda mock QR target üretilir (yarışma server'sız test)

## Faz 8 — Polish ✅ TAMAMLANDI · 2026-04-26

- [x] **ARM/DISARM confirmation dialog** — `IDialogService.ConfirmAsync` (default Cancel focus, safety)
- [x] **AlertToastHost** — `Controls/Alerts/AlertToastHost.xaml(.cs)` AlertBus subscribe, 4s popup
   - Sağ üst sidebar üstünde, level bazlı renk + ikon (Info/Warn/Danger)
- [x] **Settings persistence** — `%AppData%\GOKDOGAN\settings.json`
   - `Core/Services/Persistence/SettingsStore.cs` (Load/Save + OptionsSnapshot record)
   - 1 sn debounced save (Telemetry/Video/Map INPC değişimlerinde)
   - Boot'ta load + OnExit'te flush
- [x] **Auto-reconnect loop** — `ConnectionOrchestrator.OnPollFailed`
   - `TelemetryOptions.AutoReconnect` aktifse exponential backoff (1s, 2s, 4s, 8s, max 30s)
   - Tek seferde bir reconnect (lock), AlertBus'a "YENİDEN BAĞLANIYOR · Deneme N" mesajı
- [N/A] TopBar status pills canlı binding — Faz 3'te HzMeter/SignalRssi ile zaten canlı
- [N/A] Microsoft.Extensions.Logging — atlandı (Debug.WriteLine yeterli, AlertBus + Toast + Console mevcut)
- [N/A] FormFieldRow refactor — atlandı (görsel polish, fonksiyonel etki yok)
- [N/A] Polygon kaydet/yükle — Settings persistence içinde Faz 4 polygon dahil değil (Settings → polygon ayrı export gerek)
- [x] Mock kamikaze QR debug butonu — yalnızca aktif simülasyon backend'inde görünür

> **🔍 İnceleme noktası:** Faz 7.5 + Faz 8 agent kontrolü → bir sonraki adım.
