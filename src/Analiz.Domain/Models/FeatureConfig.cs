namespace Analiz.Domain.Entities;

public class FeatureConfig
{
    public Dictionary<string, bool> EnabledFeatures { get; set; }
    public Dictionary<string, FeatureSetting> FeatureSettings { get; set; }
    public Dictionary<string, double> NormalizationParameters { get; set; }
}