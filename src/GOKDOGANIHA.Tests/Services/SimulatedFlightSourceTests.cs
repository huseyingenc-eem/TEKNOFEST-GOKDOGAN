using System;
using System.Threading.Tasks;
using GOKDOGANIHA.Core.Models;
using GOKDOGANIHA.Core.Services.Telemetry;

namespace GOKDOGANIHA.Tests.Services;

public class SimulatedFlightSourceTests
{
    /// <summary>
    /// Simülatör Start çağrıldığında, kısa bir bekleme sonrası FlightState alanları
    /// "default" değerlerinden farklı olmalı — yani veri akışı çalışıyor.
    /// </summary>
    [Fact]
    public async Task Start_populates_FlightState_with_synthetic_data()
    {
        var state = new FlightState();
        // Default değerler: hepsi 0 (BatteryPercent 100, BatteryVoltage 0).
        Assert.Equal(0, state.Latitude);
        Assert.Equal(0, state.Longitude);
        Assert.Equal(0, state.Altitude);
        Assert.False(state.IsArmed);
        Assert.Equal(FlightMode.Manual, state.Mode);
        Assert.Equal(GpsFix.None, state.GpsFix);

        var sim = new SimulatedFlightSource(state);
        sim.Start();
        try
        {
            // Simülatör 100ms tick — 350ms beklersek en az 3 update almalıyız.
            await Task.Delay(350);

            // Yer hızı, irtifa ve enlem 0'dan farklı olmalı (sin/cos ile osile ediyor)
            Assert.NotEqual(0, state.Latitude);
            Assert.NotEqual(0, state.Longitude);
            Assert.True(state.Altitude > 0, $"Altitude should be > 0 after sim ticks, got {state.Altitude}");
            Assert.True(state.GroundSpeed > 0, $"GroundSpeed should be > 0, got {state.GroundSpeed}");

            // Yeni alanlar — Faz 0'da eklendi, sim'de besleniyor olmalı
            Assert.True(state.IsArmed);
            Assert.Equal(FlightMode.Auto, state.Mode);
            Assert.Equal(GpsFix.Fix3D, state.GpsFix);
            Assert.Equal(14, state.SatelliteCount);
            Assert.NotEqual(0, state.Airspeed);
            Assert.True(state.SignalRssi < 0, "RSSI dBm cinsinden negatif olmalı");
        }
        finally
        {
            await sim.StopAsync();
        }
    }

    /// <summary>
    /// StopAsync çağrıldıktan sonra FlightState'e yeni yazma yapılmamalı —
    /// son değer dondurulur. Sahte veri kaynağı kapatınca durur garantisi.
    /// </summary>
    [Fact]
    public async Task StopAsync_freezes_FlightState_updates()
    {
        var state = new FlightState();
        var sim = new SimulatedFlightSource(state);
        sim.Start();
        await Task.Delay(250);
        await sim.StopAsync();

        var snapshotLat = state.Latitude;
        var snapshotAlt = state.Altitude;
        var snapshotHdg = state.Heading;

        // 250ms daha bekle — sim durmuşsa hiçbir alan değişmemeli
        await Task.Delay(250);

        Assert.Equal(snapshotLat, state.Latitude);
        Assert.Equal(snapshotAlt, state.Altitude);
        Assert.Equal(snapshotHdg, state.Heading);
    }

    /// <summary>
    /// Simülatör hedef bbox veya kilit durumuna DOKUNMAMALI — kullanıcı kuralı:
    /// kamera/hedef takibi gerçek veriden gelmeli, simülatör buraya sahte veri
    /// yazmamalı. Bu test regression koruması.
    /// </summary>
    [Fact]
    public async Task Simulator_does_not_fabricate_target_or_lock_state()
    {
        var state = new FlightState();
        var sim = new SimulatedFlightSource(state);
        sim.Start();
        await Task.Delay(500);
        await sim.StopAsync();

        Assert.Equal(0, state.TargetCenterX);
        Assert.Equal(0, state.TargetCenterY);
        Assert.Equal(0, state.TargetWidth);
        Assert.Equal(0, state.TargetHeight);
        Assert.False(state.IsLocked);
        Assert.Null(state.TargetTeamNumber);
    }
}
