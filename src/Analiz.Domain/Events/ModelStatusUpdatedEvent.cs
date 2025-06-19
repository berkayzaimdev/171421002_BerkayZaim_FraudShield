using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class ModelStatusUpdatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public string NewStatus { get; }

    public ModelStatusUpdatedEvent(Guid modelId, string modelName, string newStatus)
    {
        ModelId = modelId;
        ModelName = modelName;
        NewStatus = newStatus;
    }
} 