using System;

namespace GOKDOGANIHA.Core.Models.Alerts;

/// <summary>
/// AlertBus üzerinden yayılan tek bir uyarı. Producer (Monitor) doldurur,
/// consumer (AlertConsole, Toast, Audio) tüketir. <see cref="Kind"/> stable
/// bir etiket (ör. "battery", "hss-proximity"); consumer'lar bunu filtre/grup
/// için kullanabilir. Time caller tarafından verilir — test'lerde IClock.UtcNow.
/// </summary>
public sealed record Alert(
    Guid Id,
    string Kind,
    AlertLevel Level,
    string Title,
    string Message,
    DateTime TimeUtc)
{
    public static Alert Create(string kind, AlertLevel level, string title, string message, DateTime timeUtc)
        => new(Guid.NewGuid(), kind, level, title, message, timeUtc);
}
