using System;
using System.Collections.Generic;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Core.Services.Autonomy;

/// <summary>
/// Kilitlenme denetim motoru — şartname zorunluluğu olan 5 kuralı doğrular:
/// <list type="number">
///   <item>Hedef merkez kilitlenme dikdörtgeni içinde</item>
///   <item>Boyut farkı tolerans içinde (default %6)</item>
///   <item>IoU ≥ 0.9</item>
///   <item>5 sn pencerede toplam 4 sn valid frame</item>
///   <item>Tekrar-kilit yasağı (aynı hedef art-arda)</item>
/// </list>
/// Tek <see cref="EvaluateFrame"/> çağrısı stateless gibi davranır (input → output)
/// ama pencere FIFO buffer'ı internal state'tir. Pure-ish: <see cref="IClock"/>
/// dışarıdan inject — testlerde fast-forward için.
/// </summary>
public sealed class KilitlenmeDenetim
{
    private readonly AutonomyOptions _options;
    private readonly IClock _clock;
    private readonly Queue<FrameSample> _window = new();

    private int? _currentTargetId;
    private int? _lastSuccessfullyLockedTargetId;
    private LockState _state = LockState.Idle;

    public KilitlenmeDenetim(AutonomyOptions options, IClock clock)
    {
        _options = options;
        _clock = clock;
    }

    public LockState State => _state;
    public int? CurrentTargetId => _currentTargetId;
    public int? LastLockedTargetId => _lastSuccessfullyLockedTargetId;

    /// <summary>Pencerede biriken valid süre (saniye) — ardışık iki sample arasındaki delta'larla integral.</summary>
    public double LockProgressSeconds
    {
        get
        {
            var now = _clock.UtcNow;
            TrimWindow(now);
            return ComputeAccumValidSeconds(now);
        }
    }

    public event EventHandler<LockSuccessEventArgs>? LockSucceeded;

    /// <summary>
    /// Tek bir frame için 5 kuralı doğrulayıp pencereye ekler. Şartname kuralları
    /// sağlanırsa <see cref="LockSucceeded"/> tetiklenir.
    /// </summary>
    public LockFrameResult EvaluateFrame(BoundingBox targetBox, BoundingBox lockBox, int targetId)
    {
        var now = _clock.UtcNow;

        // Hedef değiştiyse pencereyi resetle (yeni hedef için temiz başlangıç).
        if (_currentTargetId != targetId)
        {
            _window.Clear();
            _currentTargetId = targetId;
            _state = LockState.Tracking;
        }

        // 1. Merkez içinde mi?
        var center = new { X = targetBox.CenterX, Y = targetBox.CenterY };
        bool centerInside = center.X >= lockBox.Left && center.X <= lockBox.Right
                         && center.Y >= lockBox.Top  && center.Y <= lockBox.Bottom;

        // 2. Boyut toleransı
        double sizeError = SizeErrorPercent(targetBox, lockBox);
        bool sizeOk = sizeError <= _options.LockTolerancePercent;

        // 3. IoU eşiği (şartname %90)
        double iou = ComputeIoU(targetBox, lockBox);
        bool iouOk = iou >= 0.90;

        // 5. Tekrar-kilit yasağı: aynı hedefe art-arda kilitleme yok.
        bool notRepeat = _lastSuccessfullyLockedTargetId != targetId;

        bool valid = centerInside && sizeOk && iouOk && notRepeat;
        string? fail = !centerInside ? "merkez-disinda"
                     : !sizeOk       ? "boyut-tolerans-disinda"
                     : !iouOk        ? "iou-dusuk"
                     : !notRepeat    ? "tekrar-kilit-yasak"
                     : null;

        _window.Enqueue(new FrameSample(now, valid));
        TrimWindow(now);

        // 4. Pencerede toplam valid süre — ardışık sample'ların timestamp farkı ile integral.
        double accum = ComputeAccumValidSeconds(now);

        bool windowFull = _window.Count > 0
            && (now - _window.Peek().Timestamp).TotalSeconds >= _options.LockWindowSeconds - 1e-3;

        if (accum >= _options.LockRequiredSeconds && _state != LockState.Locked && notRepeat)
        {
            _state = LockState.Locked;
            _lastSuccessfullyLockedTargetId = targetId;
            LockSucceeded?.Invoke(this, new LockSuccessEventArgs(targetId, now));
        }
        else if (valid && _state != LockState.Locked)
        {
            _state = LockState.Locking;
        }
        else if (!valid && _state == LockState.Locking)
        {
            // Pencere doldu ama 4 sn valid biriktiremedi → Failed; aksi halde Tracking'e dön.
            _state = windowFull && accum < _options.LockRequiredSeconds
                ? LockState.Failed
                : LockState.Tracking;
        }

        return new LockFrameResult(
            Valid: valid,
            CenterInside: centerInside,
            SizeWithinTolerance: sizeOk,
            IouAboveThreshold: iouOk,
            IsNotRepeatLock: notRepeat,
            IoU: iou,
            SizeErrorPercent: sizeError,
            FailReason: fail);
    }

    /// <summary>Kilit başarısı kabul edildikten sonra UI buton ile yeniden başlamak isterse.</summary>
    public void Reset()
    {
        _window.Clear();
        _currentTargetId = null;
        _state = LockState.Idle;
    }

    /// <summary>Tekrar-kilit yasağını sıfırla (örn. başka hedeflerle dönüldü, eski hedef tekrar uygun).</summary>
    public void ClearRepeatLock() => _lastSuccessfullyLockedTargetId = null;

    private void TrimWindow(DateTime now)
    {
        var window = TimeSpan.FromSeconds(_options.LockWindowSeconds);
        while (_window.Count > 0 && now - _window.Peek().Timestamp > window)
            _window.Dequeue();
    }

    /// <summary>
    /// Pencere içindeki valid süreyi ardışık sample'lar arasındaki gerçek delta'larla
    /// hesaplar. Bir sample'ın "süresi" = bir sonraki sample'a (veya now'a) kadar geçen
    /// zaman; sample valid ise bu süre toplama eklenir. Hard-coded frame rate'e bağımlı
    /// değil — caller 30 Hz'de de 15 Hz'de de doğru sonuç alır.
    /// </summary>
    private double ComputeAccumValidSeconds(DateTime now)
    {
        if (_window.Count == 0) return 0;
        var arr = _window.ToArray();
        double accum = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            var end = i + 1 < arr.Length ? arr[i + 1].Timestamp : now;
            var span = (end - arr[i].Timestamp).TotalSeconds;
            if (span <= 0) continue;
            // Tek frame'lik patlamaları üst sınırla — 1/15 sn'den fazla "bir sample geçerli kaldı" sayma.
            span = Math.Min(span, 1.0 / 15.0);
            if (arr[i].Valid) accum += span;
        }
        return accum;
    }

    private static double SizeErrorPercent(BoundingBox a, BoundingBox b)
    {
        double areaA = Math.Max(1, a.Width * a.Height);
        double areaB = Math.Max(1, b.Width * b.Height);
        return Math.Abs(areaA - areaB) / Math.Max(areaA, areaB) * 100.0;
    }

    private static double ComputeIoU(BoundingBox a, BoundingBox b)
    {
        double ix1 = Math.Max(a.Left, b.Left);
        double iy1 = Math.Max(a.Top, b.Top);
        double ix2 = Math.Min(a.Right, b.Right);
        double iy2 = Math.Min(a.Bottom, b.Bottom);
        if (ix2 <= ix1 || iy2 <= iy1) return 0;
        double inter = (ix2 - ix1) * (iy2 - iy1);
        double union = a.Width * a.Height + b.Width * b.Height - inter;
        return union <= 0 ? 0 : inter / union;
    }

    private readonly record struct FrameSample(DateTime Timestamp, bool Valid);
}

public sealed class LockSuccessEventArgs : EventArgs
{
    public int TargetId { get; }
    public DateTime TimestampUtc { get; }
    public LockSuccessEventArgs(int targetId, DateTime timestampUtc)
    {
        TargetId = targetId;
        TimestampUtc = timestampUtc;
    }
}
