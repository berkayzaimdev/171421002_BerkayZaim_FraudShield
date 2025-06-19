using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class FeatureConfigurationDeactivatedEvent : DomainEvent
{
    public Guid ConfigurationId { get; }
    public string Version { get; }
    public DateTime DeactivatedAt { get; }

    public FeatureConfigurationDeactivatedEvent(Guid configurationId, string version)
    {
        ConfigurationId = configurationId;
        Version = version;
        DeactivatedAt = DateTime.UtcNow;
    }
}