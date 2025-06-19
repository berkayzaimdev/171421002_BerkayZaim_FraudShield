using System.Text.Json.Serialization;

namespace Analiz.Application.DTOs.ML;

/// <summary>
/// Ensemble konfigürasyonu için DTO (Python ile uyumlu)
/// </summary>
public class EnsembleConfigurationDTO
{
    [JsonPropertyName("lightgbmWeight")]
    public double LightGBMWeight { get; set; } = 0.7;
        
    [JsonPropertyName("pcaWeight")]
    public double PCAWeight { get; set; } = 0.3;
        
    [JsonPropertyName("threshold")]
    public double Threshold { get; set; } = 0.5;
        
    [JsonPropertyName("minConfidenceThreshold")]
    public double MinConfidenceThreshold { get; set; } = 0.8;
        
    [JsonPropertyName("enableCrossValidation")]
    public bool EnableCrossValidation { get; set; } = true;
        
    [JsonPropertyName("crossValidationFolds")]
    public int CrossValidationFolds { get; set; } = 5;
        
    [JsonPropertyName("combinationStrategy")]
    public string CombinationStrategy { get; set; } = "WeightedAverage";
        
    [JsonPropertyName("lightgbm")]
    public LightGBMConfigurationDTO LightGBMConfig { get; set; } = new LightGBMConfigurationDTO();
        
    [JsonPropertyName("pca")]
    public PCAConfigurationDTO PCAConfig { get; set; } = new PCAConfigurationDTO();
}