using FraudShield.TransactionAnalysis.Domain.Common;
using MediatR;

namespace Analiz.Domain.Events;

public class ModelUpdatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }

    public ModelUpdatedEvent(Guid modelId, string modelName)
    {
        ModelId = modelId;
        ModelName = modelName;
    }
}