using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class FeatureSetting
{
    public string Name { get; set; }
    public FeatureType Type { get; set; }
    public bool IsRequired { get; set; }
    public string TransformationType { get; set; }
    public FeatureCategory Category { get; set; }
    public Dictionary<string, string> ValidationRules { get; set; }
    public Dictionary<string, double> Parameters { get; set; }
}