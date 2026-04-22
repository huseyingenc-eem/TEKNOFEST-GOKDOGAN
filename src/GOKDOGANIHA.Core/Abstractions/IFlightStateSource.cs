using System;
using System.Threading.Tasks;

namespace GOKDOGANIHA.Core.Abstractions;

/// <summary>
/// FlightState'i besleyen arka ucun soyutlaması. Şu an SimulatedFlightSource
/// tek implementasyon; ileride MAVLink adapter eklenirse aynı interface ile
/// takılır (OCP). App composition root yalnızca interface'i bilir.
/// </summary>
public interface IFlightStateSource : IDisposable
{
    void Start();
    Task StopAsync();
}
