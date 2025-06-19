namespace Analiz.Domain.Entities;

/// <summary>
/// Model tahmin sonucu
/// </summary>
public class ModelPrediction
{
    /// <summary>
    /// Tahmin edilen sınıf (true: fraud, false: normal)
    /// </summary>
    public bool PredictedLabel { get; set; }

    /// <summary>
    /// Tahmin skoru (0-1 arası)
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Fraud olma olasılığı (0-1 arası)
    /// </summary>
    public double Probability { get; set; }

    /// <summary>
    /// Anomali skoru (PCA modeli için)
    /// </summary>
    public double AnomalyScore { get; set; }

    /// <summary>
    /// Confidence skoru
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Yeniden yapılandırma hatası (PCA için)
    /// </summary>
    public double ReconstructionError { get; set; }

    /// <summary>
    /// Kullanılan model tipi
    /// </summary>
    public string ModelType { get; set; } = string.Empty;

    /// <summary>
    /// Model versiyonu
    /// </summary>
    public string ModelVersion { get; set; } = string.Empty;

    /// <summary>
    /// Tahmin zamanı
    /// </summary>
    public DateTime PredictionTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ek metadata bilgileri
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Feature katkıları
    /// </summary>
    public Dictionary<string, double> FeatureContributions { get; set; } = new();

    /// <summary>
    /// Model performans metrikleri
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Tahmin başarılı mı?
    /// </summary>
    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Ensemble model için alt model tahminleri
    /// </summary>
    public Dictionary<string, ModelPrediction> SubPredictions { get; set; } = new();

    /// <summary>
    /// Risk seviyesi
    /// </summary>
    public string RiskLevel
    {
        get
        {
            if (Probability >= 0.8) return "Critical";
            if (Probability >= 0.6) return "High";
            if (Probability >= 0.4) return "Medium";
            if (Probability >= 0.2) return "Low";
            return "Very Low";
        }
    }

    /// <summary>
    /// Güven aralığı
    /// </summary>
    public (double Lower, double Upper) ConfidenceInterval { get; set; }

    /// <summary>
    /// Açıklama metni
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// ModelPrediction oluştur
    /// </summary>
    public static ModelPrediction Create(
        bool predictedLabel,
        double probability,
        double score = 0,
        string modelType = "Unknown")
    {
        return new ModelPrediction
        {
            PredictedLabel = predictedLabel,
            Probability = probability,
            Score = score > 0 ? score : probability,
            ModelType = modelType,
            PredictionTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Hatalı tahmin oluştur
    /// </summary>
    public static ModelPrediction CreateError(string errorMessage, string modelType = "Unknown")
    {
        return new ModelPrediction
        {
            ErrorMessage = errorMessage,
            ModelType = modelType,
            PredictionTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// PCA model inputundan dönüştür
    /// </summary>
    public static ModelPrediction FromPCAResult(
        double anomalyScore,
        double reconstructionError,
        double threshold)
    {
        var isAnomaly = anomalyScore > threshold;
        var probability = Math.Min(1.0, Math.Max(0.0, anomalyScore / (threshold * 2)));

        return new ModelPrediction
        {
            PredictedLabel = isAnomaly,
            Probability = probability,
            Score = probability,
            AnomalyScore = anomalyScore,
            ReconstructionError = reconstructionError,
            ModelType = "PCA",
            PredictionTime = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["Threshold"] = threshold,
                ["IsAnomaly"] = isAnomaly
            }
        };
    }

    /// <summary>
    /// LightGBM sonucundan dönüştür
    /// </summary>
    public static ModelPrediction FromLightGBMResult(
        double probability,
        bool predictedClass,
        Dictionary<string, double> featureImportance = null)
    {
        return new ModelPrediction
        {
            PredictedLabel = predictedClass,
            Probability = probability,
            Score = probability,
            ModelType = "LightGBM",
            PredictionTime = DateTime.UtcNow,
            FeatureContributions = featureImportance ?? new Dictionary<string, double>()
        };
    }

    /// <summary>
    /// Ensemble sonucundan dönüştür
    /// </summary>
    public static ModelPrediction FromEnsembleResult(
        double probability,
        bool predictedClass,
        Dictionary<string, ModelPrediction> subPredictions)
    {
        return new ModelPrediction
        {
            PredictedLabel = predictedClass,
            Probability = probability,
            Score = probability,
            ModelType = "Ensemble",
            PredictionTime = DateTime.UtcNow,
            SubPredictions = subPredictions ?? new Dictionary<string, ModelPrediction>()
        };
    }

    /// <summary>
    /// Tahmin özeti
    /// </summary>
    public override string ToString()
    {
        return $"ModelPrediction: Label={PredictedLabel}, Probability={Probability:F4}, " +
               $"RiskLevel={RiskLevel}, ModelType={ModelType}";
    }
}