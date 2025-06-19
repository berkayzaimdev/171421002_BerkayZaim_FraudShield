using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class ModelActivatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public string Version { get; }

    public ModelActivatedEvent(Guid modelId, string modelName, string version)
    {
        ModelId = modelId;
        ModelName = modelName;
        Version = version;
    }
}