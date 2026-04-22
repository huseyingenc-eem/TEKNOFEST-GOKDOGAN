using System;
using GOKDOGANIHA.Core.Abstractions;
using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.Core.Services.Alerts;

/// <summary>
/// In-memory publisher/subscriber. Producer'lar (Monitor) <see cref="Publish"/>
/// çağırır; consumer'lar <see cref="AlertPublished"/> event'e subscribe olur.
/// Thread-safe basit implementasyon — event invocation synchronous, çağıran
/// thread'de çalışır. UI tarafındaki consumer Dispatcher'a kendisi marshal etmeli.
/// </summary>
public sealed class AlertBus : IAlertPublisher, IAlertSubscriber
{
    private readonly object _gate = new();
    private EventHandler<Alert>? _handlers;

    public event EventHandler<Alert>? AlertPublished
    {
        add    { lock (_gate) _handlers += value; }
        remove { lock (_gate) _handlers -= value; }
    }

    public void Publish(Alert alert)
    {
        EventHandler<Alert>? snapshot;
        lock (_gate) snapshot = _handlers;
        snapshot?.Invoke(this, alert);
    }
}
