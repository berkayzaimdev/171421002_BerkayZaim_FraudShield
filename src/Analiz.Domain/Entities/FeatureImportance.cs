using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class FeatureImportance : Entity
{
    public string ModelName { get; private set; }
    public string FeatureName { get; private set; }
    public double Importance { get; private set; }
    public FeatureCategory Category { get; private set; }
    public DateTime CalculatedAt { get; private set; }
    public Dictionary<string, double> Statistics { get; private set; }

    private FeatureImportance()
    {
        Statistics = new Dictionary<string, double>();
    }

    public static FeatureImportance Create(
        string modelName,
        string featureName,
        double importance,
        FeatureCategory category,
        Dictionary<string, double> statistics = null)
    {
        return new FeatureImportance
        {
            Id = Guid.NewGuid(),
            ModelName = modelName,
            FeatureName = featureName,
            Importance = importance,
            Category = category,
            Statistics = statistics ?? new Dictionary<string, double>(),
            CalculatedAt = DateTime.UtcNow
        };
    }
}