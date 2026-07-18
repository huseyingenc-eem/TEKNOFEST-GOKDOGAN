namespace GOKDOGANIHA.Core.Abstractions;

public enum FlightSourceStatus
{
    Stopped,
    Starting,
    WaitingForData,
    Ready,
    Faulted
}

public sealed class FlightSourceStatusChangedEventArgs : EventArgs
{
    public FlightSourceStatusChangedEventArgs(FlightSourceStatus status, string message)
    {
        Status = status;
        Message = message;
    }

    public FlightSourceStatus Status { get; }
    public string Message { get; }
}

/// <summary>
/// Runtime kaynak değişimi için yaşam döngüsü ve sağlık bilgisi taşıyan genişletilmiş
/// uçuş kaynağı sözleşmesi. Eski <see cref="IFlightStateSource.Start"/> API'si test ve
/// geriye uyumluluk için korunur.
/// </summary>
public interface IManagedFlightStateSource : IFlightStateSource
{
    string Name { get; }
    bool IsRunning { get; }
    bool IsReady { get; }
    event EventHandler<FlightSourceStatusChangedEventArgs>? StatusChanged;
    Task StartAsync(CancellationToken ct = default);
}
