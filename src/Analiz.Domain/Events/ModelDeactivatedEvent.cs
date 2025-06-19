using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class ModelDeactivatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public string Version { get; }
    public DateTime DeactivatedAt { get; }

    public ModelDeactivatedEvent(Guid modelId, string modelName, string version)
    {
        ModelId = modelId;
        ModelName = modelName;
        Version = version;
        DeactivatedAt = DateTime.UtcNow;
    }
}