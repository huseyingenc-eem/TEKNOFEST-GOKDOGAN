using System;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Configuration;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Autonomy;

namespace GOKDOGANIHA.Tests.Services;

public class KilitlenmeDenetimTests
{
    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = DateTime.UnixEpoch;
        public void Advance(TimeSpan t) => UtcNow += t;
    }

    private static AutonomyOptions DefaultOptions() => new()
    {
        LockTolerancePercent = 6,
        LockWindowSeconds = 5,
        LockRequiredSeconds = 4
    };

    /// <summary>Mükemmel hizalı iki kutu — IoU=1, merkez içinde, boyut eşit.</summary>
    private static (BoundingBox target, BoundingBox lockBox) PerfectAlignment()
        => (new BoundingBox(100, 100, 80, 60), new BoundingBox(100, 100, 80, 60));

    [Fact]
    public void Rule1_center_outside_lockbox_invalidates_frame()
    {
        var (_, lockBox) = PerfectAlignment();
        // Hedef merkezi kilit kutusunun çok dışında
        var target = new BoundingBox(500, 500, 80, 60);
        var d = new KilitlenmeDenetim(DefaultOptions(), new FakeClock());

        var r = d.EvaluateFrame(target, lockBox, targetId: 7);

        Assert.False(r.Valid);
        Assert.False(r.CenterInside);
        Assert.Equal("merkez-disinda", r.FailReason);
    }

    [Fact]
    public void Rule2_size_difference_above_tolerance_invalidates_frame()
    {
        // Aynı merkez, ama hedef alanı kilit alanından çok küçük (~%50 fark)
        var target = new BoundingBox(140, 130, 40, 30);     // alan = 1200
        var lockBox = new BoundingBox(100, 100, 80, 60);    // alan = 4800
        var d = new KilitlenmeDenetim(DefaultOptions(), new FakeClock());

        var r = d.EvaluateFrame(target, lockBox, targetId: 7);

        Assert.False(r.Valid);
        Assert.True(r.CenterInside);
        Assert.False(r.SizeWithinTolerance);
        Assert.Equal("boyut-tolerans-disinda", r.FailReason);
    }

    [Fact]
    public void Rule3_iou_below_threshold_invalidates_frame()
    {
        // Çok az çakışan iki kutu (IoU < 0.9)
        var target = new BoundingBox(100, 100, 80, 60);
        var lockBox = new BoundingBox(140, 130, 80, 60);    // boyut aynı, kayık → düşük IoU
        var d = new KilitlenmeDenetim(DefaultOptions(), new FakeClock());

        var r = d.EvaluateFrame(target, lockBox, targetId: 7);

        Assert.False(r.IouAboveThreshold);
        Assert.False(r.Valid);
    }

    [Fact]
    public void Rule4_four_seconds_in_window_triggers_lock_succeeded()
    {
        var clock = new FakeClock();
        var d = new KilitlenmeDenetim(DefaultOptions(), clock);
        var (target, lockBox) = PerfectAlignment();

        int succeededCalls = 0;
        d.LockSucceeded += (_, _) => succeededCalls++;

        // 30 Hz, 4.0 sn → 120 valid frame yeterli
        for (int i = 0; i < 130; i++)
        {
            d.EvaluateFrame(target, lockBox, targetId: 42);
            clock.Advance(TimeSpan.FromMilliseconds(33.333));
        }

        Assert.Equal(1, succeededCalls);
        Assert.Equal(LockState.Locked, d.State);
        Assert.Equal(42, d.LastLockedTargetId);
    }

    [Fact]
    public void Rule5_repeat_lock_on_same_target_is_blocked()
    {
        var clock = new FakeClock();
        var d = new KilitlenmeDenetim(DefaultOptions(), clock);
        var (target, lockBox) = PerfectAlignment();

        int succeeded = 0;
        d.LockSucceeded += (_, _) => succeeded++;

        // 1. başarılı kilit
        for (int i = 0; i < 130; i++)
        {
            d.EvaluateFrame(target, lockBox, targetId: 42);
            clock.Advance(TimeSpan.FromMilliseconds(33.333));
        }
        Assert.Equal(1, succeeded);

        // Pencereyi temizleyelim (10 sn ileri)
        clock.Advance(TimeSpan.FromSeconds(10));

        // 2. aynı hedefe yeni deneme — frame'ler hep "tekrar-kilit-yasak" almalı
        for (int i = 0; i < 130; i++)
        {
            var r = d.EvaluateFrame(target, lockBox, targetId: 42);
            Assert.False(r.Valid);
            Assert.False(r.IsNotRepeatLock);
            clock.Advance(TimeSpan.FromMilliseconds(33.333));
        }
        Assert.Equal(1, succeeded); // ekstra success yok
    }

    [Fact]
    public void Window_full_without_four_seconds_valid_sets_failed_state()
    {
        var clock = new FakeClock();
        var d = new KilitlenmeDenetim(DefaultOptions(), clock);
        var (target, lockBox) = PerfectAlignment();
        // Hedef merkezini kilit kutusunun dışında bırakarak hep invalid frame üret
        var offTarget = new BoundingBox(500, 500, 80, 60);

        // Önce 1 valid frame ile state=Locking yapalım
        d.EvaluateFrame(target, lockBox, targetId: 5);
        clock.Advance(TimeSpan.FromMilliseconds(33));

        // 6 sn boyunca invalid frame'ler — pencere doldu, accum < 4 sn
        for (int i = 0; i < 180; i++)
        {
            d.EvaluateFrame(offTarget, lockBox, targetId: 5);
            clock.Advance(TimeSpan.FromMilliseconds(33));
        }

        Assert.Equal(LockState.Failed, d.State);
    }

    [Fact]
    public void Different_target_does_not_trigger_repeat_lock_block()
    {
        var clock = new FakeClock();
        var d = new KilitlenmeDenetim(DefaultOptions(), clock);
        var (target, lockBox) = PerfectAlignment();

        for (int i = 0; i < 130; i++)
        {
            d.EvaluateFrame(target, lockBox, targetId: 42);
            clock.Advance(TimeSpan.FromMilliseconds(33.333));
        }

        // Farklı hedefe geçiş — yasaklı değil
        clock.Advance(TimeSpan.FromSeconds(2));
        var r = d.EvaluateFrame(target, lockBox, targetId: 99);
        Assert.True(r.IsNotRepeatLock);
    }
}
