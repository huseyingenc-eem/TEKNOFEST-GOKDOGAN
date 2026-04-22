using System;
using GOKDOGANIHA.Core.Abstractions;

namespace GOKDOGANIHA.Core.Services.Time;

/// <summary>Production IClock — DateTime.UtcNow.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
