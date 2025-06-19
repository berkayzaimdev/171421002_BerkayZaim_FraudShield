using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class ModelMetricsUpdatedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public Dictionary<string, double> Metrics { get; }
    public DateTime UpdatedAt { get; }

    public ModelMetricsUpdatedEvent(Guid modelId, Dictionary<string, double> metrics)
    {
        ModelId = modelId;
        Metrics = metrics;
        UpdatedAt = DateTime.UtcNow;
    }
}