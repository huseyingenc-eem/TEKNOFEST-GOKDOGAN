namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Tüm strongly-typed Options'ları tek composition root olarak tutan container.
/// App startup'ta bir kez oluşturulur ve ilgili VM / servislere constructor injection
/// ile geçirilir. Static'ten kurtulmanın ilk adımı.
/// </summary>
public sealed class ApplicationOptions
{
    public GameServerOptions GameServer { get; } = new();
    public TelemetryOptions Telemetry { get; } = new();
    public VideoOptions Video { get; } = new();
    public MapOptions Map { get; } = new();
    public AlertOptions Alerts { get; } = new();
}
