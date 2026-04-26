using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Alerts.Monitors;

namespace GOKDOGANIHA.Core.Services.Autonomy;

/// <summary>
/// Kamikaze görev state machine. 4 faz + 2 terminal (Tamam/Hata) içerir.
/// TYF taahhüdü: 100 m yaklaşma irtifa, 45° dalış, 30 m pull-up. Max 2 attempt.
///
/// FSM pure — dış dünya yalnızca <see cref="StartMission"/>, <see cref="Tick"/>,
/// <see cref="ApplyQrRead"/>, <see cref="Abort"/> çağırır. Telemetri akışı UI tick'inden
/// veya MAVLink alıcısından beslenir; FSM cleanly testable.
///
/// SOLID: tek sorumluluk (state geçiş + event yayma). Sunucuya paket gönderim
/// dış handler'da — FSM sadece <see cref="MissionCompleted"/> event'i yayar.
/// </summary>
public sealed class KamikazeFsm : INotifyPropertyChanged
{
    private readonly AutonomyOptions _options;
    private readonly IClock _clock;

    private KamikazePhase _phase = KamikazePhase.Idle;
    private string _qrText = "";
    private string _statusMessage = "Görev pasif";
    private int _attemptCount;
    private DateTime? _missionStartUtc;
    private DateTime? _phaseEnteredUtc;
    private double _targetLatitude;
    private double _targetLongitude;

    public KamikazeFsm(AutonomyOptions options, IClock clock)
    {
        _options = options;
        _clock = clock;
    }

    public KamikazePhase Phase
    {
        get => _phase;
        private set
        {
            if (_phase == value) return;
            _phase = value;
            _phaseEnteredUtc = _clock.UtcNow;
            OnPropertyChanged();
            PhaseChanged?.Invoke(this, value);
        }
    }

    public string QrText { get => _qrText; private set { if (_qrText != value) { _qrText = value; OnPropertyChanged(); } } }
    public string StatusMessage { get => _statusMessage; private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } } }
    public int AttemptCount { get => _attemptCount; private set { if (_attemptCount != value) { _attemptCount = value; OnPropertyChanged(); } } }
    public DateTime? MissionStartUtc { get => _missionStartUtc; private set { if (_missionStartUtc != value) { _missionStartUtc = value; OnPropertyChanged(); } } }

    public bool IsActive => _phase is not KamikazePhase.Idle and not KamikazePhase.Tamam and not KamikazePhase.Hata;

    public event EventHandler<KamikazePhase>? PhaseChanged;
    public event EventHandler<KamikazeMissionResult>? MissionCompleted;

    /// <summary>Yeni görev başlat. QR hedef koordinatı sunucudan gelir (/api/qr_koordinati).</summary>
    public void StartMission(double targetLat, double targetLng)
    {
        _targetLatitude = targetLat;
        _targetLongitude = targetLng;
        AttemptCount = 1;
        QrText = "";
        MissionStartUtc = _clock.UtcNow;
        StatusMessage = $"İntikal — hedefe yaklaşılıyor (≥{_options.KamikazeApproachAltitudeMeters:F0} m)";
        Phase = KamikazePhase.Intikal;
    }

    /// <summary>
    /// Kullanıcı veya MAVLink failsafe abort — tek çağrı görevi sonlandırır.
    /// Idle'a değil Hata'ya geçer ki UI "iptal edildi" rengi gösterebilsin.
    /// </summary>
    public void Abort(string reason = "Manuel iptal")
    {
        if (!IsActive) return;
        StatusMessage = reason;
        Phase = KamikazePhase.Hata;
        MissionCompleted?.Invoke(this, new KamikazeMissionResult(false, QrText, reason, _clock.UtcNow));
    }

    /// <summary>
    /// Tick — telemetri akışından (örn. UI 100ms tick veya MAVLink) çağrılır.
    /// FlightState'i okuyup faz geçişlerini değerlendirir. Pure: dış side-effect yok,
    /// sadece state değişikliği + event.
    /// </summary>
    public void Tick(FlightState state)
    {
        if (!IsActive) return;

        var distanceToTarget = GeoDistance.HaversineMeters(
            state.Latitude, state.Longitude, _targetLatitude, _targetLongitude);

        switch (_phase)
        {
            case KamikazePhase.Intikal:
                // Yaklaşma irtifasında ve hedefe yakınsak (200m içine girince) dalışa başla.
                if (state.Altitude <= _options.KamikazeApproachAltitudeMeters * 1.05
                 && distanceToTarget < 250)
                {
                    StatusMessage = $"Dalış başladı — açı hedefi {_options.KamikazeDiveAngleDegrees:F0}°";
                    Phase = KamikazePhase.Dalis;
                }
                break;

            case KamikazePhase.Dalis:
                // Pull-up irtifasının ~50% altına inince QR aramaya başla; aşılırsa fail.
                if (state.Altitude < _options.KamikazePullUpAltitudeMeters * 1.5)
                {
                    StatusMessage = "QR kod aranıyor…";
                    Phase = KamikazePhase.QrAriyor;
                }
                break;

            case KamikazePhase.QrAriyor:
                // QR okundu olayını ApplyQrRead yönetir. Pull-up irtifası altına inilirse
                // pas geçilmiş kabul edilir (QR okunamadı, bir sonraki attempt veya başarısızlık).
                if (state.Altitude <= _options.KamikazePullUpAltitudeMeters)
                {
                    if (string.IsNullOrEmpty(QrText)) HandleQrMissed();
                    else                              EnterPasGec();
                }
                break;

            case KamikazePhase.QrOkundu:
                // QR okundu, pas geç fazına geçilmiş — pull-up tamamlanması beklenir.
                if (state.Altitude >= _options.KamikazeApproachAltitudeMeters * 0.5)
                {
                    StatusMessage = "Pas geçiliyor — irtifa kazanılıyor";
                    Phase = KamikazePhase.PasGec;
                }
                break;

            case KamikazePhase.PasGec:
                // Yaklaşma irtifasına ulaşınca görev tamam.
                if (state.Altitude >= _options.KamikazeApproachAltitudeMeters)
                {
                    StatusMessage = "Görev başarılı";
                    Phase = KamikazePhase.Tamam;
                    MissionCompleted?.Invoke(this, new KamikazeMissionResult(true, QrText, "Tamamlandı", _clock.UtcNow));
                }
                break;
        }
    }

    /// <summary>
    /// Kamera/YOLO veya manuel test (debug butonu) QR metni okuduğunda çağrılır.
    /// Yalnızca QrAriyor fazında geçerli; aksi halde no-op (state corruption önleme).
    /// </summary>
    public void ApplyQrRead(string qrText)
    {
        if (_phase != KamikazePhase.QrAriyor) return;
        if (string.IsNullOrWhiteSpace(qrText)) return;
        QrText = qrText.Trim();
        StatusMessage = $"QR okundu: {QrText}";
        Phase = KamikazePhase.QrOkundu;
    }

    private void EnterPasGec()
    {
        StatusMessage = "QR okundu, pas geçiliyor";
        Phase = KamikazePhase.PasGec;
    }

    private void HandleQrMissed()
    {
        if (AttemptCount >= _options.KamikazeMaxAttempts)
        {
            StatusMessage = $"QR okunamadı — max {_options.KamikazeMaxAttempts} deneme aşıldı";
            Phase = KamikazePhase.Hata;
            MissionCompleted?.Invoke(this, new KamikazeMissionResult(false, "", "QR okunamadı", _clock.UtcNow));
        }
        else
        {
            AttemptCount++;
            StatusMessage = $"QR okunamadı, yeniden deneme {AttemptCount}/{_options.KamikazeMaxAttempts}";
            Phase = KamikazePhase.Intikal;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Görev sonu sonucu — sunucuya KamikazeBilgisi paketi yapmak için.</summary>
public sealed record KamikazeMissionResult(
    bool Success,
    string QrText,
    string Reason,
    DateTime CompletedAtUtc);
