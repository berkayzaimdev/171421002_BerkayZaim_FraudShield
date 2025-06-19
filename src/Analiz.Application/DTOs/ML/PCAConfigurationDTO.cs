using System.Text.Json.Serialization;

namespace Analiz.Application.DTOs.ML;

/// <summary>
/// PCA konfigürasyonu için DTO (Python ile uyumlu)
/// </summary>
public class PCAConfigurationDTO
{
    [JsonPropertyName("componentCount")] public int ComponentCount { get; set; } = 15;

    [JsonPropertyName("explainedVarianceThreshold")]
    public double ExplainedVarianceThreshold { get; set; } = 0.98;

    [JsonPropertyName("standardizeInput")] public bool StandardizeInput { get; set; } = true;

    [JsonPropertyName("anomalyThreshold")] public double AnomalyThreshold { get; set; } = 2.5;

    [JsonPropertyName("featureColumns")]
    public List<string> FeatureColumns { get; set; } = new List<string>
    {
        "Amount", "AmountLog", "TimeSin", "TimeCos", "DayFeature", "HourFeature",
        "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10",
        "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20",
        "V21", "V22", "V23", "V24", "V25", "V26", "V27", "V28"
    };

    [JsonPropertyName("featureThresholds")]
    public Dictionary<string, double> FeatureThresholds { get; set; } = new Dictionary<string, double>
    {
        { "Amount", 2.5 },
        { "TimeVariance", 0.05 },
        { "PCASimilarity", 0.85 }
    };
}