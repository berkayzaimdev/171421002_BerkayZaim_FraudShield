using System.Text.Json.Serialization;

namespace Analiz.Application.DTOs.ML;

/// <summary>
/// LightGBM konfigürasyonu için DTO (Python ile uyumlu)
/// </summary>
public class LightGBMConfigurationDTO
{
    [JsonPropertyName("numberOfLeaves")] public int NumberOfLeaves { get; set; } = 128;

    [JsonPropertyName("minDataInLeaf")] public int MinDataInLeaf { get; set; } = 10;

    [JsonPropertyName("learningRate")] public double LearningRate { get; set; } = 0.005;

    [JsonPropertyName("numberOfTrees")] public int NumberOfTrees { get; set; } = 1000;

    [JsonPropertyName("featureFraction")] public double FeatureFraction { get; set; } = 0.8;

    [JsonPropertyName("baggingFraction")] public double BaggingFraction { get; set; } = 0.8;

    [JsonPropertyName("baggingFrequency")] public int BaggingFrequency { get; set; } = 5;

    [JsonPropertyName("l1Regularization")] public double L1Regularization { get; set; } = 0.01;

    [JsonPropertyName("l2Regularization")] public double L2Regularization { get; set; } = 0.01;

    [JsonPropertyName("earlyStoppingRound")]
    public int EarlyStoppingRound { get; set; } = 100;

    [JsonPropertyName("minGainToSplit")] public double MinGainToSplit { get; set; } = 0.0005;

    [JsonPropertyName("useClassWeights")] public bool UseClassWeights { get; set; } = true;

    [JsonPropertyName("classWeights")]
    public Dictionary<string, double> ClassWeights { get; set; } = new Dictionary<string, double>
    {
        { "0", 1.0 },
        { "1", 75.0 }
    };

    [JsonPropertyName("predictionThreshold")]
    public double PredictionThreshold { get; set; } = 0.5;

    [JsonPropertyName("featureColumns")]
    public List<string> FeatureColumns { get; set; } = new List<string>
    {
        "Amount", "AmountLog", "TimeSin", "TimeCos", "DayOfWeek", "HourOfDay",
        "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10",
        "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20",
        "V21", "V22", "V23", "V24", "V25", "V26", "V27", "V28"
    };
}