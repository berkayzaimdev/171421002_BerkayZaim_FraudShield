using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class ModelTrainingCompletedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public Dictionary<string, double> Metrics { get; }

    public ModelTrainingCompletedEvent(Guid modelId, string modelName, Dictionary<string, double> metrics)
    {
        ModelId = modelId;
        ModelName = modelName;
        Metrics = metrics;
    }
}