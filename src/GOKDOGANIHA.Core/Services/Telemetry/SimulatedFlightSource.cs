using System;
using System.Threading;
using System.Threading.Tasks;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Core.Services.Telemetry;

/// <summary>
/// Gerçek donanım gelene kadar <see cref="FlightState"/>'i tatlı bir döngüde güncelleyen
/// geçici besleyici. Merkez (41.02, 29.01) etrafında dairesel uçuş, batarya yavaş iner.
/// MAVLink adapter eklendiğinde aynı <see cref="IFlightStateSource"/> ile takılır (OCP).
/// </summary>
public sealed class SimulatedFlightSource : IFlightStateSource
{
    private readonly FlightState _state;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public SimulatedFlightSource(FlightState state) { _state = state; }

    public void Start()
    {
        if (_loop is not null) return;
        _cts = new CancellationTokenSource();

        // Sabit ve "sahte ama makul" başlangıç değerleri — UI ilk frame'de bile dolu görünsün.
        _state.IsArmed = true;
        _state.Mode = FlightMode.Auto;
        _state.GpsFix = GpsFix.Fix3D;
        _state.SatelliteCount = 14;
        _state.SignalRssi = -62;
        _state.WpDistance = 145;
        _state.TargetTeamNumber = null;

        _loop = Task.Run(() => RunLoop(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        _cts.Cancel();
        try { if (_loop is not null) await _loop; }
        catch (OperationCanceledException) { }
        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    private async Task RunLoop(CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                var t = (DateTime.UtcNow - start).TotalSeconds;

                _state.Latitude = 39.9208 + Math.Sin(t * 0.08) * 0.015;
                _state.Longitude = 32.8541 + Math.Cos(t * 0.08) * 0.018;
                _state.Altitude = 185 + Math.Sin(t * 0.3) * 8;
                _state.GroundSpeed = 24 + Math.Sin(t * 0.4) * 2;
                _state.Airspeed = _state.GroundSpeed + Math.Sin(t * 0.7) * 1.5; // ~rüzgar etkisi
                _state.VerticalSpeed = Math.Cos(t * 0.3) * 2.4;                 // climb rate ±2.4 m/s
                _state.Heading = (t * 4) % 360;
                _state.Pitch = Math.Sin(t * 0.3) * 5;
                _state.Roll = Math.Sin(t * 0.2) * 12;
                _state.BatteryPercent = Math.Max(0, (int)(100 - t * 0.1));
                _state.BatteryVoltage = Math.Max(18, (int)(25 - t * 0.015));
                _state.WpDistance = Math.Max(20, 200 - (t * 0.5) % 180);
                _state.SignalRssi = -62 + Math.Sin(t * 0.5) * 6;                // -68 ... -56 dBm
                // Hedef bbox + IsLocked + TargetTeamNumber bu simülatörden BESLENMEZ —
                // kilitlenme ve hedef takibi ileride gerçek kameradan/YOLO'dan veya
                // sunucu mock'undan gelir. UI'da bu alanlar 0 / false / null kalır.
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
    }

    /// <summary>
    /// UI shutdown path'inde async dispose hang yapmasın diye kısa timeout'lu sync stop.
    /// </summary>
    public void Dispose()
    {
        if (_cts is null) return;
        try { _cts.Cancel(); } catch { }
        try { _loop?.Wait(TimeSpan.FromMilliseconds(500)); } catch { }
        try { _cts.Dispose(); } catch { }
        _cts = null;
        _loop = null;
    }
}
