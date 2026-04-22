using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.Core.Abstractions;

/// <summary>Alert üreten tarafın gördüğü arayüz. Monitor'lar, Orchestrator vb.</summary>
public interface IAlertPublisher
{
    void Publish(Alert alert);
}
