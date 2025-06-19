using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class ModelFailedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public string Version { get; }
    public string Error { get; }
    public DateTime FailedAt { get; }

    public ModelFailedEvent(Guid modelId, string modelName, string version, string error)
    {
        ModelId = modelId;
        ModelName = modelName;
        Version = version;
        Error = error;
        FailedAt = DateTime.UtcNow;
    }
}