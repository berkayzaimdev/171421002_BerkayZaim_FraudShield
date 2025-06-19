using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Events;

public class ModelTrainingStartedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public string Version { get; }

    public ModelTrainingStartedEvent(Guid modelId, string modelName, string version)
    {
        ModelId = modelId;
        ModelName = modelName;
        Version = version;
    }
}