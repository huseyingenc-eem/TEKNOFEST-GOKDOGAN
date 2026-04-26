using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.Core.Services.Safety;

/// <summary>
/// Otonom mod'dan manuel mod'a (IsAutonomous true → false) geçiş sayısını izler.
/// Şartname: yarışmada "manuel geçiş" kontenjanı belirlenmiş olabilir (örn. 2/5).
/// SafetyPanel'da X/5 gösterimi için kullanılır.
/// </summary>
public sealed class ManualTransitionCounter : INotifyPropertyChanged, IDisposable
{
    private readonly FlightState _state;
    private bool _previousAutonomous;
    private int _count;

    public ManualTransitionCounter(FlightState state)
    {
        _state = state;
        _previousAutonomous = state.IsAutonomous;
        _state.PropertyChanged += OnStateChanged;
    }

    public int Count
    {
        get => _count;
        private set { if (_count != value) { _count = value; OnPropertyChanged(); } }
    }

    /// <summary>Yarışma başlangıcında veya manuel reset için.</summary>
    public void Reset()
    {
        Count = 0;
        _previousAutonomous = _state.IsAutonomous;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FlightState.IsAutonomous)) return;
        // AUTO (true) → MANUAL (false) transition'ı say. Tersi sayılmaz.
        if (_previousAutonomous && !_state.IsAutonomous) Count++;
        _previousAutonomous = _state.IsAutonomous;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => _state.PropertyChanged -= OnStateChanged;
}
