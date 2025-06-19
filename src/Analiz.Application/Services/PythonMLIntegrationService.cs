using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

public class PythonMLIntegrationService
{
    private readonly ILogger<PythonMLIntegrationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _pythonPath;
    private readonly string _modelsPath;
    private readonly string _pythonScriptsPath;
    private readonly string _dataPath;

    // Business-optimized thresholds - Enhanced
    private static readonly Dictionary<string, double> BusinessThresholds = new()
    {
        ["lightgbm"] = 0.12,
        ["pca"] = 0.08,
        ["ensemble"] = 0.15
    };

    public PythonMLIntegrationService(ILogger<PythonMLIntegrationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Yapılandırma değerlerini al
        _pythonPath = _configuration["ML:Python:ExecutablePath"] ?? "python";
        _modelsPath = _configuration["ML:Python:ModelsPath"] ??
                      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Models");
        _pythonScriptsPath = _configuration["ML:Python:ScriptsPath"] ??
                             Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Python");
        _dataPath = _configuration["ML:Python:DataPath"] ??
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Data");

        // Dizinlerin varlığını kontrol et
        EnsureDirectoriesExist();
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_modelsPath);

        if (!Directory.Exists(_pythonScriptsPath))
        {
            _logger.LogWarning("Python scripts directory does not exist: {Path}", _pythonScriptsPath);
        }

        Directory.CreateDirectory(_dataPath);
    }

    /// <summary>
    /// Python model eğitimi
    /// </summary>
    public async Task<(bool Success, string ModelPath, Dictionary<string, double> Metrics)> TrainModelAsync(
        string modelType, string configJson, string dataPath)
    {
        try
        {
            _logger.LogInformation("Python ile {ModelType} model eğitimi başlatılıyor", modelType);

            // Konfigürasyon dosyasını oluştur
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var configPath = Path.Combine(_modelsPath, $"{modelType}_config_{timestamp}.json");

            await File.WriteAllTextAsync(configPath, configJson);

            // Veri setinin varlığını kontrol et
            if (!File.Exists(dataPath))
            {
                _logger.LogError("Veri seti bulunamadı: {DataPath}", dataPath);
                return (false, null, null);
            }

            // Python betiğini çalıştır
            var modelOutputDir = Path.Combine(_modelsPath, $"{modelType}_{timestamp}");
            Directory.CreateDirectory(modelOutputDir);

            var scriptPath = Path.Combine(_pythonScriptsPath, "fraud_detection_models.py");

            var args = new StringBuilder();
            args.Append($" --data \"{dataPath}\"");
            args.Append($" --config \"{configPath}\"");
            args.Append($" --output \"{modelOutputDir}\"");
            args.Append($" --model-type {modelType}");

            // Python'u çalıştır
            var (exitCode, output, error) = await RunPythonProcessAsync(scriptPath, args.ToString());

            if (exitCode != 0)
            {
                _logger.LogError("Python eğitim hatası! Çıkış kodu: {ExitCode}, Hata: {Error}", exitCode, error);
                return (false, null, null);
            }

            _logger.LogInformation("Python eğitimi tamamlandı: {Output}", output);

            // Model bilgi dosyasını bul
            var modelInfoFiles = Directory.GetFiles(modelOutputDir, "model_info_*.json");
            if (modelInfoFiles.Length == 0)
            {
                _logger.LogError("Model bilgi dosyası bulunamadı: {Dir}", modelOutputDir);
                return (false, null, null);
            }

            // En son oluşturulan model bilgi dosyasını al
            var modelInfoPath = modelInfoFiles[0];
            var modelInfo = await File.ReadAllTextAsync(modelInfoPath);
            var modelInfoJson = JsonDocument.Parse(modelInfo);

            // Model yolunu ve metrikleri çıkart
            var modelPath = modelInfoJson.RootElement.GetProperty("model_path").GetString();
            var metrics = ParseMetrics(modelInfoJson.RootElement.GetProperty("metrics"));

            _logger.LogInformation("Model eğitildi ve kaydedildi: {ModelPath}", modelPath);

            return (true, modelPath, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Python model eğitimi sırasında hata oluştu");
            return (false, null, null);
        }
    }

    /// <summary>
    /// Python ile tahmin yap - BUSINESS ENHANCED
    /// </summary>
    public async Task<ModelPrediction> PredictAsync(ModelInput input, string modelInfoPath,
        string modelType = "ensemble")
    {
        try
        {
            _logger.LogInformation("Enhanced Python tahmin yapılıyor. Model: {ModelInfoPath}, Tip: {ModelType}",
                modelInfoPath, modelType);

            // Model info dosyasının varlığını kontrol et
            if (!File.Exists(modelInfoPath))
            {
                throw new FileNotFoundException($"Model bilgi dosyası bulunamadı: {modelInfoPath}");
            }

            // Enhanced input preparation - C# tarafında business feature'ları hazırla
            var enhancedInputJson = await PrepareEnhancedInput(input, modelType);

            var inputPath = Path.Combine(_modelsPath, $"enhanced_input_{Guid.NewGuid():N}.json");
            var outputPath = Path.Combine(_modelsPath, $"enhanced_output_{Guid.NewGuid():N}.json");

            await File.WriteAllTextAsync(inputPath, enhancedInputJson);

            try
            {
                // Python betiğini çalıştır
                var scriptPath = Path.Combine(_pythonScriptsPath, "fraud_prediction.py");

                var args = new StringBuilder();
                args.Append($" --model-info \"{modelInfoPath}\"");
                args.Append($" --input \"{inputPath}\"");
                args.Append($" --output \"{outputPath}\"");
                args.Append($" --model-type {modelType}");

                // Python'u çalıştır
                var (exitCode, output, error) = await RunPythonProcessAsync(scriptPath, args.ToString());

                if (exitCode != 0)
                {
                    _logger.LogError("Enhanced Python tahmin hatası! Çıkış kodu: {ExitCode}, Hata: {Error}", exitCode, error);

                    // Enhanced fallback prediction döndür
                    return CreateEnhancedFallbackPrediction(input, modelType, $"Python error: {error}");
                }

                // Çıktı dosyasını kontrol et
                if (!File.Exists(outputPath))
                {
                    _logger.LogWarning("Enhanced tahmin sonucu dosyası bulunamadı, fallback kullanılıyor");
                    return CreateEnhancedFallbackPrediction(input, modelType, "Output file not found");
                }

                // Tahmin sonucunu oku
                var predictionJson = await File.ReadAllTextAsync(outputPath);

                if (string.IsNullOrWhiteSpace(predictionJson))
                {
                    _logger.LogWarning("Boş enhanced tahmin sonucu, fallback kullanılıyor");
                    return CreateEnhancedFallbackPrediction(input, modelType, "Empty prediction result");
                }

                var prediction = JsonSerializer.Deserialize<EnhancedPythonPredictionResult>(predictionJson);

                // Güvenlik kontrolleri
                if (prediction == null ||
                    prediction.probability == null ||
                    prediction.probability.Length == 0 ||
                    prediction.predicted_class == null ||
                    prediction.predicted_class.Length == 0)
                {
                    _logger.LogWarning("Geçersiz enhanced tahmin sonucu, fallback kullanılıyor");
                    return CreateEnhancedFallbackPrediction(input, modelType, "Invalid prediction structure");
                }

                // Enhanced ModelPrediction'a dönüştür
                var result = CreateEnhancedModelPredictionFromPython(prediction, modelType, output);

                // Enhanced debug bilgileri logla
                _logger.LogInformation("Enhanced Python tahmin başarılı: Model={ModelType}, " +
                                       "OriginalProb={OriginalProb:F4}, EnhancedProb={Probability:F4}, " +
                                       "BusinessThreshold={BusinessThreshold:F3}, Confidence={Confidence:F2}",
                    result.ModelType, 
                    GetOriginalProbability(prediction),
                    result.Probability, 
                    GetBusinessThreshold(prediction, modelType),
                    result.Confidence);

                return result;
            }
            finally
            {
                // Geçici dosyaları temizle
                try
                {
                    if (File.Exists(inputPath)) File.Delete(inputPath);
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Enhanced geçici dosyalar temizlenirken hata oluştu");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enhanced Python tahmin sırasında hata oluştu");

            // Enhanced son fallback
            return CreateEnhancedFallbackPrediction(input, modelType, ex.Message);
        }
    }

    /// <summary>
    /// Enhanced input hazırla - Business features ile
    /// </summary>
    private async Task<string> PrepareEnhancedInput(ModelInput input, string modelType)
    {
        var enhancedInput = new Dictionary<string, object>
        {
            // Base features
            ["time"] = input.Time,
            ["amount"] = input.Amount,
            
            // V features
            ["v1"] = input.V1, ["v2"] = input.V2, ["v3"] = input.V3, ["v4"] = input.V4,
            ["v5"] = input.V5, ["v6"] = input.V6, ["v7"] = input.V7, ["v8"] = input.V8,
            ["v9"] = input.V9, ["v10"] = input.V10, ["v11"] = input.V11, ["v12"] = input.V12,
            ["v13"] = input.V13, ["v14"] = input.V14, ["v15"] = input.V15, ["v16"] = input.V16,
            ["v17"] = input.V17, ["v18"] = input.V18, ["v19"] = input.V19, ["v20"] = input.V20,
            ["v21"] = input.V21, ["v22"] = input.V22, ["v23"] = input.V23, ["v24"] = input.V24,
            ["v25"] = input.V25, ["v26"] = input.V26, ["v27"] = input.V27, ["v28"] = input.V28,
            
            // Enhanced features - C# tarafında hesaplanan
            ["amountLog"] = Math.Log(1 + input.Amount),
            
            // Time-based features
            ["timeSin"] = Math.Sin(2 * Math.PI * input.Time / (24 * 60 * 60)),
            ["timeCos"] = Math.Cos(2 * Math.PI * input.Time / (24 * 60 * 60)),
            ["dayFeature"] = (int)((input.Time / (24 * 60 * 60)) % 7),
            ["hourFeature"] = (int)((input.Time / 3600) % 24),
            
            // Business context - Python'a hint olarak
            ["businessContext"] = new
            {
                ExpectedThreshold = BusinessThresholds.GetValueOrDefault(modelType, 0.5),
                IsHighAmount = input.Amount > 0.8,
                IsNightTime = ((input.Time / 3600) % 24) < 6 || ((input.Time / 3600) % 24) > 22,
                VRiskScore = CalculateVRiskScore(input),
                ModelType = modelType
            }
        };

        return JsonSerializer.Serialize(enhancedInput, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Enhanced Python sonucundan ModelPrediction oluştur
    /// </summary>
    private ModelPrediction CreateEnhancedModelPredictionFromPython(EnhancedPythonPredictionResult prediction, 
        string modelType, string pythonOutput)
    {
        var probability = Math.Max(0.0, Math.Min(1.0, prediction.probability[0]));
        var businessThreshold = prediction.business_threshold ?? BusinessThresholds.GetValueOrDefault(modelType, 0.5);
        var enhancedConfidence = prediction.confidence ?? CalculateEnhancedConfidence(probability, modelType);

        var result = new ModelPrediction
        {
            // Enhanced temel tahmin bilgileri
            PredictedLabel = prediction.predicted_class[0] == 1,
            Probability = probability,
            Score = probability,
            AnomalyScore = prediction.anomaly_score?.Length > 0 ? prediction.anomaly_score[0] : probability * 2,

            // Enhanced model bilgileri
            ModelType = $"Enhanced_{modelType}",
            PredictionTime = DateTime.UtcNow,
            Confidence = enhancedConfidence,

            // Hata durumu kontrolü
            ErrorMessage = prediction.error_message ?? string.Empty,

            // Enhanced metadata
            Metadata = new Dictionary<string, object>
            {
                ["ModelType"] = modelType,
                ["BusinessEnhanced"] = true,
                ["BusinessThreshold"] = businessThreshold,
                ["OriginalProbability"] = GetOriginalProbability(prediction),
                ["BusinessAdjustments"] = prediction.business_adjustments ?? new Dictionary<string, object>(),
                ["EnhancedConfidence"] = enhancedConfidence,
                ["Timestamp"] = DateTime.UtcNow,
                ["PythonOutput"] = pythonOutput,
                ["PredictionMethod"] = prediction.method ?? "unknown"
            }
        };

        // Model tipine göre enhanced özel işlemler
        switch (modelType.ToLower())
        {
            case "ensemble":
                ProcessEnhancedEnsembleResponse(result, prediction);
                break;
            case "lightgbm":
                ProcessEnhancedLightGBMResponse(result, prediction);
                break;
            case "pca":
                ProcessEnhancedPCAResponse(result, prediction);
                break;
        }

        // Enhanced fallback kullanım durumunu kontrol et
        if (prediction.method != null && prediction.method.Contains("fallback"))
        {
            result.Metadata["FallbackUsed"] = true;
            result.Metadata["FallbackReason"] = prediction.fallback_reason ?? "Unknown";
            _logger.LogWarning("Enhanced Python tarafında fallback kullanıldı: {ModelType}, Method: {Method}",
                modelType, prediction.method);
        }

        return result;
    }

    /// <summary>
    /// Enhanced Ensemble response işle
    /// </summary>
    private void ProcessEnhancedEnsembleResponse(ModelPrediction result, EnhancedPythonPredictionResult prediction)
    {
        result.Metadata["ModelDetails"] = new Dictionary<string, object>
        {
            ["EnsembleProbability"] = result.Probability,
            ["EnsembleScore"] = result.Score,
            ["EnsembleAnomalyScore"] = result.AnomalyScore,
            ["DynamicWeightingUsed"] = prediction.weight_strategy != null,
            ["WeightStrategy"] = prediction.weight_strategy ?? "standard",
            ["ModelAgreement"] = prediction.model_agreement ?? 0.5
        };

        // Enhanced alt model tahminlerini ekle
        if (prediction.lightgbm_probability?.Length > 0)
        {
            result.Metadata["LightGBM_Probability"] = prediction.lightgbm_probability[0];
            result.Metadata["ModelDetails_LightGBM"] = new
            {
                Probability = prediction.lightgbm_probability[0],
                Source = "Enhanced_LightGBM_SubModel",
                Confidence = prediction.lightgbm_confidence ?? 0.5
            };
        }

        if (prediction.pca_probability?.Length > 0)
        {
            result.Metadata["PCA_Probability"] = prediction.pca_probability[0];
            result.Metadata["PCA_AnomalyScore"] = result.AnomalyScore;
            result.Metadata["ModelDetails_PCA"] = new
            {
                Probability = prediction.pca_probability[0],
                AnomalyScore = result.AnomalyScore,
                Source = "Enhanced_PCA_SubModel"
            };
        }

        // Enhanced ensemble ağırlıkları
        if (prediction.model_weights != null)
        {
            result.Metadata["DynamicWeights"] = prediction.model_weights;
        }

        if (prediction.original_weights != null)
        {
            result.Metadata["OriginalWeights"] = prediction.original_weights;
        }

        result.Metadata["ModelDescription"] = "Enhanced ensemble model with dynamic weighting and business rules";
    }

    /// <summary>
    /// Enhanced LightGBM response işle
    /// </summary>
    private void ProcessEnhancedLightGBMResponse(ModelPrediction result, EnhancedPythonPredictionResult prediction)
    {
        result.Metadata["ModelDetails"] = new Dictionary<string, object>
        {
            ["LightGBM_Probability"] = result.Probability,
            ["LightGBM_Score"] = result.Score,
            ["EnhancedClassification"] = true,
            ["BusinessRulesApplied"] = prediction.business_adjustments != null
        };

        // Enhanced feature importance
        if (prediction.feature_importance != null)
        {
            result.FeatureContributions = prediction.feature_importance;
            result.Metadata["FeatureImportanceAvailable"] = true;
        }

        result.Metadata["ModelDescription"] = "Enhanced LightGBM with business rules and optimized thresholds";
    }

    /// <summary>
    /// Enhanced PCA response işle
    /// </summary>
    private void ProcessEnhancedPCAResponse(ModelPrediction result, EnhancedPythonPredictionResult prediction)
    {
        result.Metadata["ModelDetails"] = new Dictionary<string, object>
        {
            ["PCA_Probability"] = result.Probability,
            ["PCA_AnomalyScore"] = result.AnomalyScore,
            ["EnhancedAnomalyDetection"] = true,
            ["BusinessOptimized"] = true
        };

        // Enhanced reconstruction error
        if (prediction.reconstruction_error?.Length > 0)
        {
            result.ReconstructionError = prediction.reconstruction_error[0];
            result.Metadata["ReconstructionError"] = result.ReconstructionError;
        }

        result.Metadata["ModelDescription"] = "Enhanced PCA with business-optimized thresholds";
    }

    /// <summary>
    /// Enhanced fallback tahmin oluştur
    /// </summary>
    private ModelPrediction CreateEnhancedFallbackPrediction(ModelInput input, string modelType, string errorMessage)
    {
        try
        {
            _logger.LogInformation("Enhanced fallback tahmin oluşturuluyor: {ModelType}", modelType);

            var businessThreshold = BusinessThresholds.GetValueOrDefault(modelType, 0.5);

            // Enhanced rule-based tahmin
            double probability = 0.08; // Lower base for more conservative approach

            // Enhanced amount risk calculation
            var normalizedAmount = Math.Min(1.0, (double)input.Amount);
            var amountRisk = normalizedAmount > 0.8 ? normalizedAmount * 0.35 : normalizedAmount * 0.15;
            probability += amountRisk;

            // Enhanced V risk calculation
            var vRiskScore = CalculateVRiskScore(input);
            probability += vRiskScore * 0.4;

            // Enhanced time risk
            var hourOfDay = ((int)(input.Time / 3600)) % 24;
            if (hourOfDay < 6 || hourOfDay > 22)
            {
                probability += 0.12; // Night time significant boost
            }
            else if (hourOfDay >= 9 && hourOfDay <= 17)
            {
                probability -= 0.03; // Business hours slight penalty
            }

            // Model-specific adjustments
            probability = modelType.ToLower() switch
            {
                "ensemble" => probability * 1.05, // Slight boost for ensemble fallback
                "lightgbm" => probability * 1.0,
                "pca" => probability * 0.95, // More conservative for PCA
                _ => probability
            };

            // Enhanced constraints
            probability = Math.Max(0.02, Math.Min(0.88, probability));

            var isHighRisk = probability >= businessThreshold;
            var enhancedConfidence = CalculateEnhancedConfidence(probability, modelType) * 0.6; // Reduced for fallback

            return new ModelPrediction
            {
                PredictedLabel = isHighRisk,
                Probability = probability,
                Score = probability,
                AnomalyScore = probability * 2.2, // Enhanced anomaly calculation
                ModelType = $"Enhanced_{modelType}_Fallback",
                ErrorMessage = $"Enhanced fallback used: {errorMessage}",
                PredictionTime = DateTime.UtcNow,
                Confidence = enhancedConfidence,
                Metadata = new Dictionary<string, object>
                {
                    ["IsEnhancedFallback"] = true,
                    ["OriginalModelType"] = modelType,
                    ["BusinessThreshold"] = businessThreshold,
                    ["ErrorMessage"] = errorMessage,
                    ["FallbackReason"] = "Enhanced Python prediction failed",
                    ["FallbackMethod"] = "Enhanced_Rule_Based",
                    ["EnhancedCalculation"] = true,
                    ["ModelDetails"] = new Dictionary<string, object>
                    {
                        [$"Enhanced_{modelType}_Probability"] = probability,
                        [$"Enhanced_{modelType}_Score"] = probability,
                        ["EnhancedFallbackComponents"] = new
                        {
                            BaseRisk = 0.08,
                            AmountRisk = amountRisk,
                            VRiskScore = vRiskScore,
                            VRiskContribution = vRiskScore * 0.4,
                            TimeRisk = (hourOfDay < 6 || hourOfDay > 22) ? 0.12 : 
                                      (hourOfDay >= 9 && hourOfDay <= 17) ? -0.03 : 0.0,
                            ModelAdjustment = modelType.ToLower() switch
                            {
                                "ensemble" => 1.05,
                                "lightgbm" => 1.0,
                                "pca" => 0.95,
                                _ => 1.0
                            }
                        }
                    },
                    ["Timestamp"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception fallbackEx)
        {
            _logger.LogError(fallbackEx, "Enhanced fallback tahmin oluşturulurken hata");

            // Enhanced minimal fallback
            var businessThreshold = BusinessThresholds.GetValueOrDefault(modelType, 0.5);
            
            return new ModelPrediction
            {
                PredictedLabel = false,
                Probability = 0.20, // More conservative
                Score = 0.20,
                AnomalyScore = 0.45,
                ModelType = $"Enhanced_{modelType}_CriticalFallback",
                ErrorMessage = $"Enhanced critical fallback: {fallbackEx.Message}",
                PredictionTime = DateTime.UtcNow,
                Confidence = 0.25,
                Metadata = new Dictionary<string, object>
                {
                    ["IsEnhancedCriticalFallback"] = true,
                    ["OriginalError"] = errorMessage,
                    ["FallbackError"] = fallbackEx.Message,
                    ["BusinessThreshold"] = businessThreshold,
                    ["ModelDetails"] = new Dictionary<string, object>
                    {
                        ["EnhancedCriticalFallback_Probability"] = 0.20,
                        ["EnhancedCriticalFallback_Score"] = 0.20
                    }
                }
            };
        }
    }

    /// <summary>
    /// V feature risk skoru hesapla - Enhanced
    /// </summary>
    private double CalculateVRiskScore(ModelInput input)
    {
        var riskScore = 0.0;
        var riskCount = 0;

        // Enhanced risk thresholds
        var highRiskChecks = new (string property, double threshold, double weight)[]
        {
            (nameof(input.V1), -2.0, 1.0),
            (nameof(input.V2), 2.0, 0.9),
            (nameof(input.V3), -3.0, 1.1),
            (nameof(input.V4), -1.0, 0.8),
            (nameof(input.V10), -3.0, 1.2),
            (nameof(input.V14), -4.0, 1.3) // Highest weight for V14
        };

        foreach (var (property, threshold, weight) in highRiskChecks)
        {
            var value = property switch
            {
                nameof(input.V1) => input.V1,
                nameof(input.V2) => input.V2,
                nameof(input.V3) => input.V3,
                nameof(input.V4) => input.V4,
                nameof(input.V10) => input.V10,
                nameof(input.V14) => input.V14,
                _ => 0
            };

            if ((threshold < 0 && value < threshold) || (threshold > 0 && value > threshold))
            {
                riskScore += weight;
                riskCount++;
            }
        }

        // Normalize and apply multiplier for multiple risks
        var normalizedScore = riskScore / highRiskChecks.Sum(x => x.weight);
        
        // Bonus for multiple extreme values
        if (riskCount >= 3)
        {
            normalizedScore *= 1.3; // 30% boost for multiple extreme values
        }

        return Math.Min(1.0, normalizedScore);
    }

    /// <summary>
    /// Enhanced confidence hesapla
    /// </summary>
    private double CalculateEnhancedConfidence(double probability, string modelType)
    {
        // Enhanced base confidence
        var baseConfidence = modelType.ToLower() switch
        {
            "ensemble" => 0.88,
            "lightgbm" => 0.78,
            "pca" => 0.68,
            _ => 0.60
        };

        // Enhanced extremity bonus
        var extremityBonus = Math.Abs(probability - 0.5) * 0.25;
        
        // Business threshold consideration
        var businessThreshold = BusinessThresholds.GetValueOrDefault(modelType, 0.5);
        var thresholdDistance = Math.Abs(probability - businessThreshold);
        var thresholdBonus = thresholdDistance > 0.2 ? 0.08 : 0.0;

        var finalConfidence = baseConfidence + extremityBonus + thresholdBonus;
        return Math.Max(0.35, Math.Min(0.95, finalConfidence));
    }

    /// <summary>
    /// Helper metodlar
    /// </summary>
    private double GetOriginalProbability(EnhancedPythonPredictionResult prediction)
    {
        if (prediction.business_adjustments is JsonElement adjustments &&
            adjustments.TryGetProperty("original_probability", out var originalElement))
        {
            return originalElement.GetDouble();
        }
        return prediction.probability?[0] ?? 0.0;
    }

    private double GetBusinessThreshold(EnhancedPythonPredictionResult prediction, string modelType)
    {
        return prediction.business_threshold ?? BusinessThresholds.GetValueOrDefault(modelType, 0.5);
    }

    /// <summary>
    /// Enhanced Python tahmin sonucu sınıfı
    /// </summary>
    private class EnhancedPythonPredictionResult
    {
        public double[]? probability { get; set; }
        public int[]? predicted_class { get; set; }
        public double[]? score { get; set; }
        public double[]? anomaly_score { get; set; }
        public double[]? lightgbm_probability { get; set; }
        public double[]? pca_probability { get; set; }
        public double[]? reconstruction_error { get; set; }
        public double threshold_used { get; set; }
        public double? business_threshold { get; set; }
        public double? confidence { get; set; }
        public int pca_components { get; set; }
        public int feature_count { get; set; }
        public string? method { get; set; }
        public string? fallback_reason { get; set; }
        public string? error_message { get; set; }
        public bool fallback_used { get; set; }
        public bool error_fallback { get; set; }

        // Enhanced fields
        public object? business_adjustments { get; set; }
        public Dictionary<string, double>? model_weights { get; set; }
        public Dictionary<string, double>? original_weights { get; set; }
        public Dictionary<string, double>? feature_importance { get; set; }
        public string? weight_strategy { get; set; }
        public double? lightgbm_confidence { get; set; }
        public double? model_agreement { get; set; }
        public Dictionary<string, object>? debug_info { get; set; }
    }

    /// <summary>
    /// Python betiğini çalıştır
    /// </summary>
    private async Task<(int ExitCode, string Output, string Error)> RunPythonProcessAsync(string scriptPath,
        string arguments)
    {
        try
        {
            _logger.LogDebug("Enhanced Python betiği çalıştırılıyor: {Script} {Args}", scriptPath, arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{scriptPath}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    _logger.LogTrace("Enhanced Python: {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    _logger.LogTrace("Enhanced Python Error: {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enhanced Python betiği çalıştırılırken hata oluştu");
            return (-1, string.Empty, ex.ToString());
        }
    }

    /// <summary>
    /// JSON'dan metrikleri çıkar
    /// </summary>
    private Dictionary<string, double> ParseMetrics(JsonElement metricsElement)
    {
        var metrics = new Dictionary<string, double>();

        foreach (var property in metricsElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Number)
            {
                metrics[property.Name] = property.Value.GetDouble();
            }
        }

        return metrics;
    }
    #region Advanced ML Methods - Minimal Extension

/// <summary>
/// Advanced model eğitimi - mevcut TrainModelAsync'in genişletilmiş versiyonu
/// </summary>
public async Task<(bool Success, string ModelPath, Dictionary<string, double> Metrics)> TrainAdvancedModelAsync(
    string modelType, string configJson, string dataPath)
{
    try
    {
        _logger.LogInformation("Advanced model eğitimi başlatılıyor: {ModelType}", modelType);

        // Konfigürasyon dosyasını oluştur
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var configPath = Path.Combine(_modelsPath, $"advanced_{modelType}_config_{timestamp}.json");

        await File.WriteAllTextAsync(configPath, configJson);

        // Veri setinin varlığını kontrol et
        if (!File.Exists(dataPath))
        {
            _logger.LogError("Veri seti bulunamadı: {DataPath}", dataPath);
            return (false, null, null);
        }

        // Python betiğini çalıştır - advanced_ml_models.py
        var modelOutputDir = Path.Combine(_modelsPath, $"advanced_{modelType}_{timestamp}");
        Directory.CreateDirectory(modelOutputDir);

        // advanced_ml_models.py script'ini kullan
        var scriptPath = Path.Combine(_pythonScriptsPath, "advanced_ml_models.py");

        var args = new StringBuilder();
        args.Append($" --data \"{dataPath}\"");
        args.Append($" --config \"{configPath}\"");
        args.Append($" --output \"{modelOutputDir}\"");
        args.Append($" --model-type {modelType}");

        // Python'u çalıştır
        var (exitCode, output, error) = await RunPythonProcessAsync(scriptPath, args.ToString());

        if (exitCode != 0)
        {
            _logger.LogError("Advanced Python eğitim hatası! Çıkış kodu: {ExitCode}, Hata: {Error}", exitCode, error);
            return (false, null, null);
        }

        _logger.LogInformation("Advanced Python eğitimi tamamlandı: {Output}", output);

        // Model bilgi dosyasını bul
        var modelInfoFiles = Directory.GetFiles(modelOutputDir, "model_info_*.json");
        if (modelInfoFiles.Length == 0)
        {
            _logger.LogError("Advanced model bilgi dosyası bulunamadı: {Dir}", modelOutputDir);
            return (false, null, null);
        }

        // En son oluşturulan model bilgi dosyasını al
        var modelInfoPath = modelInfoFiles[0];
        var modelInfo = await File.ReadAllTextAsync(modelInfoPath);
        var modelInfoJson = JsonDocument.Parse(modelInfo);

        // Model yolunu ve metrikleri çıkart
        var modelPath = modelInfoJson.RootElement.GetProperty("model_path").GetString();
        var metrics = ParseMetrics(modelInfoJson.RootElement.GetProperty("metrics"));

        _logger.LogInformation("Advanced model eğitildi ve kaydedildi: {ModelPath}", modelPath);

        return (true, modelPath, metrics);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Advanced model eğitimi sırasında hata oluştu: {ModelType}", modelType);
        return (false, null, null);
    }
}

/// <summary>
/// Veri dengeleme ile model eğitimi
/// </summary>
public async Task<(bool Success, string ModelPath, Dictionary<string, double> Metrics)> TrainWithDataBalancingAsync(
    string modelType, string configJson, string dataPath, string balancingMethod)
{
    try
    {
        _logger.LogInformation("Veri dengeleme ile model eğitimi: {ModelType}, Method: {Method}", modelType, balancingMethod);

        // Konfigürasyon dosyasını oluştur
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var configPath = Path.Combine(_modelsPath, $"{modelType}_balanced_{balancingMethod}_config_{timestamp}.json");

        await File.WriteAllTextAsync(configPath, configJson);

        // Python betiğini çalıştır
        var modelOutputDir = Path.Combine(_modelsPath, $"{modelType}_balanced_{balancingMethod}_{timestamp}");
        Directory.CreateDirectory(modelOutputDir);

        var scriptPath = Path.Combine(_pythonScriptsPath, "advanced_ml_models.py");

        var args = new StringBuilder();
        args.Append($" --data \"{dataPath}\"");
        args.Append($" --config \"{configPath}\"");
        args.Append($" --output \"{modelOutputDir}\"");
        args.Append($" --model-type {modelType}");
        args.Append($" --balance-method {balancingMethod}");

        // Python'u çalıştır
        var (exitCode, output, error) = await RunPythonProcessAsync(scriptPath, args.ToString());

        if (exitCode != 0)
        {
            _logger.LogError("Veri dengeleme ile eğitim hatası! Çıkış kodu: {ExitCode}, Hata: {Error}", exitCode, error);
            return (false, null, null);
        }

        _logger.LogInformation("Veri dengeleme ile eğitim tamamlandı: {Output}", output);

        // Model bilgi dosyasını bul ve oku
        var modelInfoFiles = Directory.GetFiles(modelOutputDir, "model_info_*.json");
        if (modelInfoFiles.Length == 0)
        {
            _logger.LogError("Model bilgi dosyası bulunamadı: {Dir}", modelOutputDir);
            return (false, null, null);
        }

        var modelInfoPath = modelInfoFiles[0];
        var modelInfo = await File.ReadAllTextAsync(modelInfoPath);
        var modelInfoJson = JsonDocument.Parse(modelInfo);

        var modelPath = modelInfoJson.RootElement.GetProperty("model_path").GetString();
        var metrics = ParseMetrics(modelInfoJson.RootElement.GetProperty("metrics"));

        _logger.LogInformation("Dengelenmiş model eğitildi: {ModelPath}", modelPath);

        return (true, modelPath, metrics);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Veri dengeleme ile model eğitimi hatası: {ModelType}", modelType);
        return (false, null, null);
    }
}

/// <summary>
/// Advanced model ile tahmin - mevcut PredictAsync'in genişletilmiş versiyonu
/// </summary>
public async Task<ModelPrediction> PredictAdvancedAsync(ModelInput input, string modelInfoPath, string modelType)
{
    try
    {
        _logger.LogInformation("Advanced model ile tahmin: Model={ModelInfoPath}, Tip={ModelType}", modelInfoPath, modelType);

        // Model info dosyasının varlığını kontrol et
        if (!File.Exists(modelInfoPath))
        {
            throw new FileNotFoundException($"Advanced model bilgi dosyası bulunamadı: {modelInfoPath}");
        }

        // Input'u hazırla
        var inputJson = await PrepareAdvancedInput(input, modelType);

        var inputPath = Path.Combine(_modelsPath, $"advanced_input_{Guid.NewGuid():N}.json");
        var outputPath = Path.Combine(_modelsPath, $"advanced_output_{Guid.NewGuid():N}.json");

        await File.WriteAllTextAsync(inputPath, inputJson);

        try
        {
            // Python betiğini çalıştır - fraud_prediction.py (mevcut script kullanılabilir)
            var scriptPath = Path.Combine(_pythonScriptsPath, "fraud_prediction.py");

            var args = new StringBuilder();
            args.Append($" --model-info \"{modelInfoPath}\"");
            args.Append($" --input \"{inputPath}\"");
            args.Append($" --output \"{outputPath}\"");
            args.Append($" --model-type {modelType}");

            var (exitCode, output, error) = await RunPythonProcessAsync(scriptPath, args.ToString());

            if (exitCode != 0)
            {
                _logger.LogError("Advanced Python tahmin hatası! Çıkış kodu: {ExitCode}, Hata: {Error}", exitCode, error);
                return CreateAdvancedFallbackPrediction(input, modelType, $"Python error: {error}");
            }

            // Çıktı dosyasını oku
            if (!File.Exists(outputPath))
            {
                _logger.LogWarning("Advanced tahmin sonucu dosyası bulunamadı, fallback kullanılıyor");
                return CreateAdvancedFallbackPrediction(input, modelType, "Output file not found");
            }

            var predictionJson = await File.ReadAllTextAsync(outputPath);
            if (string.IsNullOrWhiteSpace(predictionJson))
            {
                _logger.LogWarning("Boş advanced tahmin sonucu, fallback kullanılıyor");
                return CreateAdvancedFallbackPrediction(input, modelType, "Empty prediction result");
            }

            var prediction = JsonSerializer.Deserialize<AdvancedPythonPredictionResult>(predictionJson);

            if (prediction?.probability == null || prediction.probability.Length == 0)
            {
                _logger.LogWarning("Geçersiz advanced tahmin sonucu, fallback kullanılıyor");
                return CreateAdvancedFallbackPrediction(input, modelType, "Invalid prediction structure");
            }

            // ModelPrediction'a dönüştür
            var result = new ModelPrediction
            {
                PredictedLabel = prediction.predicted_class?[0] == 1,
                Probability = Math.Max(0.0, Math.Min(1.0, prediction.probability[0])),
                Score = prediction.probability[0],
                AnomalyScore = prediction.anomaly_score?.Length > 0 ? prediction.anomaly_score[0] : prediction.probability[0] * 2,
                ModelType = $"Advanced_{modelType}",
                PredictionTime = DateTime.UtcNow,
                Confidence = prediction.confidence ?? 0.7,
                ErrorMessage = prediction.error_message ?? string.Empty,
                
                Metadata = new Dictionary<string, object>
                {
                    ["IsAdvancedModel"] = true,
                    ["AdvancedModelType"] = modelType,
                    ["PredictionMethod"] = prediction.method ?? "advanced",
                    ["BusinessThreshold"] = GetBusinessThreshold(modelType),
                    ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };

            _logger.LogInformation("Advanced tahmin başarılı: Model={ModelType}, Probability={Probability:F4}",
                modelType, result.Probability);

            return result;
        }
        finally
        {
            // Geçici dosyaları temizle
            try
            {
                if (File.Exists(inputPath)) File.Delete(inputPath);
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Advanced geçici dosyalar temizlenirken hata");
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Advanced tahmin sırasında hata: {ModelType}", modelType);
        return CreateAdvancedFallbackPrediction(input, modelType, ex.Message);
    }
}

private async Task<string> PrepareAdvancedInput(ModelInput input, string modelType)
{
    var advancedInput = new Dictionary<string, object>
    {
        // Temel features
        ["time"] = input.Time,
        ["amount"] = input.Amount,
        
        // V features
        ["v1"] = input.V1, ["v2"] = input.V2, ["v3"] = input.V3, ["v4"] = input.V4,
        ["v5"] = input.V5, ["v6"] = input.V6, ["v7"] = input.V7, ["v8"] = input.V8,
        ["v9"] = input.V9, ["v10"] = input.V10, ["v11"] = input.V11, ["v12"] = input.V12,
        ["v13"] = input.V13, ["v14"] = input.V14, ["v15"] = input.V15, ["v16"] = input.V16,
        ["v17"] = input.V17, ["v18"] = input.V18, ["v19"] = input.V19, ["v20"] = input.V20,
        ["v21"] = input.V21, ["v22"] = input.V22, ["v23"] = input.V23, ["v24"] = input.V24,
        ["v25"] = input.V25, ["v26"] = input.V26, ["v27"] = input.V27, ["v28"] = input.V28,
        
        // Advanced features
        ["amountLog"] = Math.Log(1 + input.Amount),
        ["timeSin"] = Math.Sin(2 * Math.PI * input.Time / (24 * 60 * 60)),
        ["timeCos"] = Math.Cos(2 * Math.PI * input.Time / (24 * 60 * 60)),
        ["dayOfWeek"] = (int)((input.Time / (24 * 60 * 60)) % 7),
        ["hourOfDay"] = (int)((input.Time / 3600) % 24),
        
        // Model context
        ["modelType"] = modelType,
        ["businessThreshold"] = GetBusinessThreshold(modelType)
    };

    return JsonSerializer.Serialize(advancedInput, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}

private ModelPrediction CreateAdvancedFallbackPrediction(ModelInput input, string modelType, string errorMessage)
{
    _logger.LogInformation("Advanced fallback prediction oluşturuluyor: {ModelType}", modelType);

    var businessThreshold = GetBusinessThreshold(modelType);

    // Basit rule-based prediction
    double probability = 0.1;
    
    // V-risk hesaplama
    var vRisk = CalculateVRisk(input);
    probability += vRisk * 0.4;
    
    // Amount risk
    var amountRisk = Math.Min(1.0, input.Amount) > 0.8 ? 0.3 : Math.Min(1.0, input.Amount) * 0.2;
    probability += amountRisk;
    
    // Time risk
    var hourOfDay = ((int)(input.Time / 3600)) % 24;
    if (hourOfDay < 6 || hourOfDay > 22) probability += 0.15;

    probability = Math.Max(0.02, Math.Min(0.85, probability));

    return new ModelPrediction
    {
        PredictedLabel = probability >= businessThreshold,
        Probability = probability,
        Score = probability,
        AnomalyScore = probability * 2.0,
        ModelType = $"Advanced_{modelType}_Fallback",
        ErrorMessage = $"Advanced fallback: {errorMessage}",
        PredictionTime = DateTime.UtcNow,
        Confidence = 0.5,
        
        Metadata = new Dictionary<string, object>
        {
            ["IsAdvancedFallback"] = true,
            ["OriginalModelType"] = modelType,
            ["BusinessThreshold"] = businessThreshold,
            ["ErrorMessage"] = errorMessage
        }
    };
}
/// <summary>
/// Data path'i döndür - Advanced modeller için
/// </summary>
public string GetDataPath()
{
    var csvPath = Path.Combine(_dataPath, "creditcard.csv");
    
    if (File.Exists(csvPath))
    {
        _logger.LogInformation("Data path bulundu: {Path}", csvPath);
        return csvPath;
    }
    
    // Alternatif lokasyonları dene
    var alternativePaths = new[]
    {
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "creditcard.csv"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "creditcard.csv"),
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "creditcard.csv"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "Data", "creditcard.csv")
    };
    
    foreach (var altPath in alternativePaths)
    {
        if (File.Exists(altPath))
        {
            _logger.LogInformation("Alternative data path bulundu: {Path}", altPath);
            return altPath;
        }
    }
    
    _logger.LogWarning("Hiçbir data path bulunamadı. Configured path: {ConfiguredPath}", csvPath);
    return csvPath; // En azından configured path'i döndür
}

private double GetBusinessThreshold(string modelType)
{
    return modelType.ToLower() switch
    {
        "attention" => 0.18,
        "autoencoder" => 0.12,
        "isolation_forest" => 0.10,
        "gan_augmented" => 0.14,
        _ => 0.15
    };
}

private double CalculateVRisk(ModelInput input)
{
    var riskCount = 0;
    var thresholds = new (string prop, double threshold)[]
    {
        (nameof(input.V1), -2.0), (nameof(input.V2), 2.0), (nameof(input.V3), -3.0),
        (nameof(input.V4), -1.0), (nameof(input.V10), -3.0), (nameof(input.V14), -4.0)
    };

    foreach (var (prop, threshold) in thresholds)
    {
        var value = prop switch
        {
            nameof(input.V1) => input.V1,
            nameof(input.V2) => input.V2,
            nameof(input.V3) => input.V3,
            nameof(input.V4) => input.V4,
            nameof(input.V10) => input.V10,
            nameof(input.V14) => input.V14,
            _ => 0
        };

        if ((threshold < 0 && value < threshold) || (threshold > 0 && value > threshold))
            riskCount++;
    }

    return Math.Min(1.0, riskCount / (double)thresholds.Length);
}

/// <summary>
/// Advanced Python prediction result sınıfı
/// </summary>
private class AdvancedPythonPredictionResult
{
    public double[]? probability { get; set; }
    public int[]? predicted_class { get; set; }
    public double[]? score { get; set; }
    public double[]? anomaly_score { get; set; }
    public double? confidence { get; set; }
    public string? method { get; set; }
    public string? error_message { get; set; }
}

#endregion
}