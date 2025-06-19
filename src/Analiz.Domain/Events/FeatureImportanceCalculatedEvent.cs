using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class FeatureImportanceCalculatedEvent : DomainEvent
{
    public Guid FeatureImportanceId { get; }
    public string ModelName { get; }
    public string FeatureName { get; }
    public double Importance { get; }
    public DateTime CalculatedAt { get; }

    public FeatureImportanceCalculatedEvent(
        Guid featureImportanceId,
        string modelName,
        string featureName,
        double importance)
    {
        FeatureImportanceId = featureImportanceId;
        ModelName = modelName;
        FeatureName = featureName;
        Importance = importance;
        CalculatedAt = DateTime.UtcNow;
    }
}