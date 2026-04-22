using System;

namespace GOKDOGANIHA.Core.Abstractions;

/// <summary>
/// Zaman soyutlaması — test'lerde fake-clock ile fast-forward yapabilmek için.
/// Her zaman bağımlı servis (Monitor, Orchestrator) IClock üzerinden okuyacak.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
