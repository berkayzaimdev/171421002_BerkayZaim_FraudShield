using Analiz.Application.Exceptions;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.Evaluation;
using Analiz.Domain.Events;
using FraudShield.TransactionAnalysis.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System.Linq;

namespace Analiz.Application.Services
{
    public class ModelService : IModelService
    {
        private readonly IModelRepository _modelRepository;
        private readonly IModelEvaluator _modelEvaluator;
        private readonly ILogger<ModelService> _logger;
        private readonly IPublisher _publisher;
        private readonly PythonMLIntegrationService _pythonService;
        private readonly IConfiguration _configuration;
        private readonly string _dataPath;

        // Enhanced Business Thresholds
        private static readonly Dictionary<string, double> BusinessThresholds = new()
        {
            ["lightgbm"] = 0.12,
            ["pca"] = 0.08,
            ["ensemble"] = 0.15
        };

        public ModelService(
            IModelRepository modelRepository,
            IModelEvaluator modelEvaluator,
            ILogger<ModelService> logger,
            IPublisher publisher,
            PythonMLIntegrationService pythonService,
            IConfiguration configuration)
        {
            _modelRepository = modelRepository;
            _modelEvaluator = modelEvaluator;
            _logger = logger;
            _publisher = publisher;
            _pythonService = pythonService;
            _configuration = configuration;
            _dataPath = _configuration["ML:Python:DataPath"] ??
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Data");
        }

        /// <summary>
        /// Model eğitimi - Enhanced
        /// </summary>
        public async Task<TrainingResult> TrainModelAsync(TrainingRequest request)
        {
            try
            {
                _logger.LogInformation("Enhanced model eğitimi başlatılıyor: {ModelName}, Tip: {ModelType}",
                    request.ModelName, request.ModelType);

                var modelMetadata = ModelMetadata.Create(
                    request.ModelName,
                    GenerateVersion(),
                    request.ModelType,
                    request.Configuration);

                var csvPath = await EnsureDatasetExists(request.ModelType);

                var (success, modelPath, rawMetrics) = await _pythonService.TrainModelAsync(
                    request.ModelType.ToString().ToLower(),
                    request.Configuration,
                    csvPath);

                if (!success || modelPath == null)
                {
                    _logger.LogError("Enhanced model eğitimi başarısız oldu");
                    modelMetadata.MarkAsFailed("Enhanced Python model eğitimi başarısız oldu");
                    await _modelRepository.SaveModelMetadataAsync(modelMetadata);
                    throw new ModelTrainingException("Enhanced Python model eğitimi başarısız oldu");
                }

                var enhancedMetrics = ProcessEnhancedMetrics(rawMetrics);
                modelMetadata.UpdateMetrics(rawMetrics);

                await _modelRepository.SaveModelFileAsync(modelMetadata.Id, modelPath);
                await _modelRepository.SaveModelMetadataAsync(modelMetadata);

                _logger.LogInformation("Enhanced model eğitimi tamamlandı: {ModelName}, F1: {F1:F4}, AUC: {AUC:F4}",
                    request.ModelName, enhancedMetrics.F1Score, enhancedMetrics.AUC);

                return new TrainingResult
                {
                    ModelId = modelMetadata.Id,
                    Metrics = enhancedMetrics,
                    TrainingTime = DateTime.Now - modelMetadata.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced model eğitimi sırasında hata: {Message}", ex.Message);
                throw new ModelTrainingException($"Enhanced model eğitimi hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enhanced Ensemble model eğitimi
        /// </summary>
        public async Task<TrainingResult> TrainEnsembleModelAsync(TrainingRequest request)
        {
            try
            {
                _logger.LogInformation("Enhanced Ensemble model eğitimi başlatılıyor: {ModelName}", request.ModelName);

                var modelMetadata = ModelMetadata.Create(
                    request.ModelName,
                    GenerateVersion(),
                    ModelType.Ensemble,
                    request.Configuration);

                var csvPath = await EnsureDatasetExists(ModelType.Ensemble);

                var (success, modelPath, rawMetrics) = await _pythonService.TrainModelAsync(
                    "ensemble",
                    request.Configuration,
                    csvPath);

                if (!success || modelPath == null)
                {
                    _logger.LogError("Enhanced Ensemble model eğitimi başarısız oldu");
                    modelMetadata.MarkAsFailed("Enhanced Python ensemble model eğitimi başarısız oldu");
                    await _modelRepository.SaveModelMetadataAsync(modelMetadata);
                    throw new ModelTrainingException("Enhanced Python ensemble model eğitimi başarısız oldu");
                }

                var enhancedMetrics = ProcessEnhancedMetrics(rawMetrics);
                modelMetadata.UpdateMetrics(rawMetrics);

                await _modelRepository.SaveModelFileAsync(modelMetadata.Id, modelPath);
                await _modelRepository.SaveModelMetadataAsync(modelMetadata);

                _logger.LogInformation("Enhanced Ensemble eğitimi tamamlandı: F1: {F1:F4}, AUC: {AUC:F4}",
                    enhancedMetrics.F1Score, enhancedMetrics.AUC);

                return new TrainingResult
                {
                    ModelId = modelMetadata.Id,
                    Metrics = enhancedMetrics,
                    TrainingTime = DateTime.Now - modelMetadata.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced Ensemble model eğitimi sırasında hata: {Message}", ex.Message);
                throw new ModelTrainingException($"Enhanced Ensemble model eğitimi hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enhanced Python ile tahmin yap - Business Logic ile
        /// </summary>
        public async Task<ModelPrediction> PredictAsync(string modelName, ModelInput input, ModelType type)
        {
            try
            {
                _logger.LogInformation("Enhanced tahmin yapılıyor - Model: {ModelName}, Tip: {ModelType}", modelName, type);

                var modelMetadata = await _modelRepository.GetActiveModelAsync(type);
                if (modelMetadata == null)
                {
                    _logger.LogWarning("Aktif model bulunamadı: {ModelName}, enhanced fallback kullanılıyor", modelName);
                    return CreateEnhancedAlternativePrediction(input, modelName, "No active model found", type);
                }

                var modelInfoPath = await _modelRepository.GetModelInfoPath(modelMetadata.Id);
                if (string.IsNullOrEmpty(modelInfoPath) || !File.Exists(modelInfoPath))
                {
                    _logger.LogWarning("Model bilgi dosyası bulunamadı: {ModelName}, enhanced fallback kullanılıyor", modelName);
                    return CreateEnhancedAlternativePrediction(input, modelName, "Model info file not found", type);
                }

                try
                {
                    var modelType = DetermineModelTypeFromEnum(type);
                    
                    // Enhanced Python prediction with business logic
                    var prediction = await _pythonService.PredictAsync(input, modelInfoPath, modelType);

                    // Model metadata güncelleme
                    modelMetadata.LastUsedAt = DateTime.Now;
                    await _modelRepository.UpdateModelMetadataAsync(modelMetadata);

                    // Enhanced prediction metadata
                    if (prediction.Metadata == null)
                        prediction.Metadata = new Dictionary<string, object>();

                    prediction.Metadata["ModelId"] = modelMetadata.Id;
                    prediction.Metadata["ModelName"] = modelName;
                    prediction.Metadata["ModelVersion"] = modelMetadata.Version;
                    prediction.Metadata["EnhancedPrediction"] = true;
                    prediction.Metadata["BusinessThreshold"] = BusinessThresholds.GetValueOrDefault(modelType, 0.5);
                    prediction.Metadata["UsedAt"] = DateTime.UtcNow;

                    // Enhanced logging
                    _logger.LogInformation("Enhanced tahmin başarılı - Model: {ModelName}, " +
                                           "Probability: {Probability:F4}, Confidence: {Confidence:F2}, " +
                                           "BusinessEnhanced: {Enhanced}",
                        modelName, prediction.Probability, prediction.Confidence,
                        prediction.Metadata.GetValueOrDefault("BusinessEnhanced", false));

                    return prediction;
                }
                catch (Exception pythonEx)
                {
                    _logger.LogError(pythonEx, "Enhanced Python entegrasyonu hatası - Model: {ModelName}, fallback kullanılıyor",
                        modelName);
                    return CreateEnhancedAlternativePrediction(input, modelName,
                        $"Enhanced Python integration error: {pythonEx.Message}", type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced tahmin sırasında genel hata: {ModelName}", modelName);
                return CreateEnhancedAlternativePrediction(input, modelName, $"Enhanced general prediction error: {ex.Message}", type);
            }
        }

        /// <summary>
        /// Enhanced PCA prediction
        /// </summary>
        public async Task<ModelPrediction> PredictAsync(string modelName, PCAModelInput pcaInput)
        {
            try
            {
                _logger.LogInformation("Enhanced PCA tahmin yapılıyor - Model: {ModelName}", modelName);
                var modelInput = ModelInput.FromPCAModelInput(pcaInput);
                return await PredictAsync(modelName, modelInput, ModelType.PCA);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced PCA tahmin sırasında hata: {ModelName}", modelName);
                throw;
            }
        }

        /// <summary>
        /// Enhanced Alternative Prediction - Business Logic ile
        /// </summary>
        private ModelPrediction CreateEnhancedAlternativePrediction(ModelInput input, string modelName, string reason, ModelType modelType)
        {
            try
            {
                _logger.LogInformation("Enhanced alternative prediction oluşturuluyor: {ModelName}", modelName);

                var businessThreshold = BusinessThresholds.GetValueOrDefault(modelType.ToString().ToLower(), 0.5);

                // Enhanced rule-based prediction
                double baseProbability = 0.06; // Conservative base
                double probability = baseProbability;

                // Enhanced V-risk calculation
                var vRiskScore = CalculateEnhancedVRiskScore(input);
                if (vRiskScore > 0.3)
                {
                    var vRiskBoost = vRiskScore * 0.4;
                    probability += vRiskBoost;
                    _logger.LogDebug("Enhanced V-risk detected: {VRiskScore:F3}, boost: +{Boost:F3}", vRiskScore, vRiskBoost);
                }

                // Enhanced amount risk
                var amountRisk = CalculateEnhancedAmountRisk(input.Amount);
                probability += amountRisk;

                // Enhanced time-based risk
                var timeRisk = CalculateEnhancedTimeRisk(input.Time);
                probability += timeRisk;

                // ModelType-specific adjustments
                probability = modelType switch
                {
                    ModelType.Ensemble => probability * 1.08, // Ensemble gets slight boost
                    ModelType.LightGBM => probability * 1.0,
                    ModelType.PCA => probability * 0.92, // PCA more conservative
                    _ => probability
                };

                // Enhanced constraints
                probability = Math.Max(0.01, Math.Min(0.87, probability));

                // Business threshold application
                var isHighRisk = probability >= businessThreshold;
                var enhancedConfidence = CalculateEnhancedConfidence(probability, modelType.ToString().ToLower()) * 0.65; // Reduced for fallback

                return new ModelPrediction
                {
                    PredictedLabel = isHighRisk,
                    Probability = probability,
                    Score = probability,
                    AnomalyScore = probability * 2.3, // Enhanced anomaly score
                    ModelType = $"Enhanced_{modelType}_Alternative",
                    ErrorMessage = $"Enhanced alternative used: {reason}",
                    PredictionTime = DateTime.UtcNow,
                    Confidence = enhancedConfidence,
                    
                    Metadata = new Dictionary<string, object>
                    {
                        ["IsEnhancedAlternative"] = true,
                        ["OriginalModelName"] = modelName,
                        ["OriginalModelType"] = modelType.ToString(),
                        ["BusinessThreshold"] = businessThreshold,
                        ["ErrorMessage"] = reason,
                        ["FallbackReason"] = "Enhanced Python prediction unavailable",
                        ["FallbackMethod"] = "Enhanced_Business_Rule_Based",
                        ["EnhancedCalculation"] = true,
                        ["ModelDetails"] = new Dictionary<string, object>
                        {
                            [$"Enhanced_{modelType}_Probability"] = probability,
                            [$"Enhanced_{modelType}_Score"] = probability,
                            ["EnhancedFallbackComponents"] = new
                            {
                                BaseProbability = baseProbability,
                                VRiskScore = vRiskScore,
                                VRiskContribution = vRiskScore * 0.4,
                                AmountRisk = amountRisk,
                                TimeRisk = timeRisk,
                                ModelTypeAdjustment = modelType switch
                                {
                                    ModelType.Ensemble => 1.08,
                                    ModelType.LightGBM => 1.0,
                                    ModelType.PCA => 0.92,
                                    _ => 1.0
                                }
                            }
                        },
                        ["Timestamp"] = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced alternative prediction oluşturulurken hata");

                var businessThreshold = BusinessThresholds.GetValueOrDefault(modelType.ToString().ToLower(), 0.5);
                
                return new ModelPrediction
                {
                    PredictedLabel = false,
                    Probability = 0.18, // More conservative
                    Score = 0.18,
                    AnomalyScore = 0.4,
                    ModelType = $"Enhanced_{modelType}_CriticalFallback",
                    ErrorMessage = $"Enhanced critical fallback: {ex.Message}",
                    PredictionTime = DateTime.UtcNow,
                    Confidence = 0.25,
                    
                    Metadata = new Dictionary<string, object>
                    {
                        ["IsEnhancedCriticalFallback"] = true,
                        ["OriginalError"] = reason,
                        ["FallbackError"] = ex.Message,
                        ["BusinessThreshold"] = businessThreshold,
                        ["ModelDetails"] = new Dictionary<string, object>
                        {
                            ["EnhancedCriticalFallback_Probability"] = 0.18,
                            ["EnhancedCriticalFallback_Score"] = 0.18
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Enhanced V-risk score calculation
        /// </summary>
        private double CalculateEnhancedVRiskScore(ModelInput input)
        {
            var riskScore = 0.0;
            var riskCount = 0;

            // Enhanced weighted risk checks
            var enhancedRiskChecks = new (string property, double threshold, double weight)[]
            {
                (nameof(input.V1), -2.0, 1.1),
                (nameof(input.V2), 2.0, 0.95),
                (nameof(input.V3), -3.0, 1.15),
                (nameof(input.V4), -1.0, 0.85),
                (nameof(input.V10), -3.0, 1.25),
                (nameof(input.V14), -4.0, 1.35) // Highest weight
            };

            foreach (var (property, threshold, weight) in enhancedRiskChecks)
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

            // Normalize
            var normalizedScore = riskScore / enhancedRiskChecks.Sum(x => x.weight);
            
            // Enhanced multiplier for multiple risks
            if (riskCount >= 3)
            {
                normalizedScore *= 1.4; // 40% boost
            }
            else if (riskCount >= 2)
            {
                normalizedScore *= 1.2; // 20% boost
            }

            return Math.Min(1.0, normalizedScore);
        }

        /// <summary>
        /// Enhanced amount risk calculation
        /// </summary>
        private double CalculateEnhancedAmountRisk(float amount)
        {
            var normalizedAmount = Math.Min(1.0, amount);
            
            if (normalizedAmount > 0.9) return 0.45;
            if (normalizedAmount > 0.8) return 0.35;
            if (normalizedAmount > 0.6) return 0.25;
            if (normalizedAmount > 0.4) return 0.15;
            if (normalizedAmount > 0.2) return 0.08;
            
            return 0.02;
        }

        /// <summary>
        /// Enhanced time risk calculation
        /// </summary>
        private double CalculateEnhancedTimeRisk(float time)
        {
            if (time <= 0) return 0.0;

            var hourOfDay = ((int)(time / 3600)) % 24;
            
            // Enhanced time-based risk
            if (hourOfDay < 5 || hourOfDay > 23) return 0.15; // Very late/early hours
            if (hourOfDay < 7 || hourOfDay > 22) return 0.12; // Late/early hours
            if (hourOfDay >= 9 && hourOfDay <= 17) return -0.02; // Business hours slight penalty
            
            return 0.0;
        }

        /// <summary>
        /// Enhanced confidence calculation
        /// </summary>
        private double CalculateEnhancedConfidence(double probability, string modelType)
        {
            var baseConfidence = modelType switch
            {
                "ensemble" => 0.86,
                "lightgbm" => 0.76,
                "pca" => 0.66,
                _ => 0.58
            };

            var extremityBonus = Math.Abs(probability - 0.5) * 0.28;
            var finalConfidence = baseConfidence + extremityBonus;
            
            return Math.Max(0.3, Math.Min(0.92, finalConfidence));
        }

        // Deprecated methodlar - Enhanced versiyonlar için wrapper
        public ModelPrediction PredictSingle(ITransformer model, ModelInput input)
        {
            _logger.LogWarning("PredictSingle deprecated, Enhanced PredictAsync kullanın.");
            return CreateEnhancedAlternativePrediction(input, "Legacy", "Deprecated method used", ModelType.Ensemble);
        }

        public ModelPrediction PredictSinglePCA(ITransformer model, PCAModelInput input)
        {
            throw new NotImplementedException("PredictSinglePCA deprecated, Enhanced PredictAsync kullanın.");
        }

        public Task<ITransformer> GetModelTransformerAsync(string modelName)
        {
            throw new NotImplementedException("Enhanced Python integration kullanılıyor.");
        }

        public Task<(ITransformer Transformer, string ConfigJson)> GetModelWithConfigAsync(string modelName)
        {
            throw new NotImplementedException("Enhanced Python integration kullanılıyor.");
        }

        // Diğer metodlar aynı kalıyor
        private ModelMetrics ProcessEnhancedMetrics(Dictionary<string, double> rawMetrics)
        {
            var metrics = new ModelMetrics();

            try
            {
                // Temel metrikler
                metrics.Accuracy = rawMetrics.GetValueOrDefault("accuracy", 0);
                metrics.Precision = rawMetrics.GetValueOrDefault("precision", 0);
                metrics.Recall = rawMetrics.GetValueOrDefault("recall", 0);
                metrics.F1Score = rawMetrics.GetValueOrDefault("f1_score", 0);
                metrics.AUC = rawMetrics.GetValueOrDefault("auc", 0);
                metrics.AUCPR = rawMetrics.GetValueOrDefault("auc_pr", 0);

                // Confusion Matrix
                metrics.TruePositive = (int)rawMetrics.GetValueOrDefault("true_positive", 0);
                metrics.TrueNegative = (int)rawMetrics.GetValueOrDefault("true_negative", 0);
                metrics.FalsePositive = (int)rawMetrics.GetValueOrDefault("false_positive", 0);
                metrics.FalseNegative = (int)rawMetrics.GetValueOrDefault("false_negative", 0);

                // Enhanced business metrics
                metrics.OptimalThreshold = rawMetrics.GetValueOrDefault("optimal_threshold", 0.5);
                
                // Business threshold karşılaştırması
                var modelType = rawMetrics.ContainsKey("model_type") ? rawMetrics["model_type"].ToString() : "ensemble";
                var businessThreshold = BusinessThresholds.GetValueOrDefault(modelType, 0.5);
                
                if (metrics.OptimalThreshold < businessThreshold)
                {
                    _logger.LogInformation("Model optimal threshold ({OptimalThreshold:F4}) < Business threshold ({BusinessThreshold:F4})", 
                        metrics.OptimalThreshold, businessThreshold);
                }

                // Diğer metrikler...
                metrics.Specificity = rawMetrics.GetValueOrDefault("specificity", 0);
                metrics.Sensitivity = rawMetrics.GetValueOrDefault("sensitivity", 0);
                metrics.BalancedAccuracy = rawMetrics.GetValueOrDefault("balanced_accuracy", 0);
                metrics.MatthewsCorrCoef = rawMetrics.GetValueOrDefault("matthews_corrcoef", 0);
                metrics.CohenKappa = rawMetrics.GetValueOrDefault("cohen_kappa", 0);

                // Enhanced additional metrics
                foreach (var kvp in rawMetrics)
                {
                    if (!IsBasicMetric(kvp.Key))
                    {
                        metrics.AdditionalMetrics[kvp.Key] = kvp.Value;
                    }
                }

                _logger.LogInformation(
                    "Enhanced metrikleri işlendi. Accuracy: {Accuracy:F4}, F1: {F1:F4}, AUC: {AUC:F4}, OptimalThreshold: {OptimalThreshold:F4}",
                    metrics.Accuracy, metrics.F1Score, metrics.AUC, metrics.OptimalThreshold);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced metrik işleme sırasında hata oluştu");
                
                // Minimal fallback metrics
                metrics.Accuracy = rawMetrics.GetValueOrDefault("accuracy", 0);
                metrics.Precision = rawMetrics.GetValueOrDefault("precision", 0);
                metrics.Recall = rawMetrics.GetValueOrDefault("recall", 0);
                metrics.F1Score = rawMetrics.GetValueOrDefault("f1_score", 0);
                metrics.AUC = rawMetrics.GetValueOrDefault("auc", 0);

                return metrics;
            }
        }

        private bool IsBasicMetric(string metricName)
        {
            var basicMetrics = new HashSet<string>
            {
                "accuracy", "precision", "recall", "f1_score", "auc", "auc_pr",
                "true_positive", "true_negative", "false_positive", "false_negative",
                "specificity", "sensitivity", "balanced_accuracy", "matthews_corrcoef", "cohen_kappa"
            };

            return basicMetrics.Contains(metricName.ToLower());
        }

        public async Task<ModelMetrics> GetModelMetricsAsync(string modelName)
        {
            try
            {
                var modelMetadata = await _modelRepository.GetActiveModelAsync(modelName);
                if (modelMetadata == null)
                    throw new ModelNotFoundException(modelName);

                return ProcessEnhancedMetrics(modelMetadata.Metrics);
            }
            catch (ModelNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced model metrikleri alınırken hata: {ModelName}", modelName);
                throw new ModelMetricsException($"Enhanced model metrikleri hatası: {ex.Message}", ex);
            }
        }

        public async Task<EvaluationResult> EvaluateModelAsync(EvaluationRequest request)
        {
            try
            {
                var modelMetadata = await _modelRepository.GetModelAsync(request.ModelName, request.Version);
                if (modelMetadata == null)
                    throw new ModelNotFoundException(request.ModelName, request.Version);

                var enhancedMetrics = ProcessEnhancedMetrics(modelMetadata.Metrics);

                return new EvaluationResult
                {
                    ModelId = modelMetadata.Id,
                    Metrics = enhancedMetrics,
                    EvaluationTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced model değerlendirme sırasında hata: {ModelName}", request.ModelName);
                throw new ModelEvaluationException($"Enhanced model değerlendirme hatası: {ex.Message}", ex);
            }
        }

        // Helper metodlar
        public async Task<bool> UpdateModelAsync(string modelName, ModelUpdateRequest request)
        {
            var model = await _modelRepository.GetModelAsync(modelName, request.Version);
            if (model == null) return false;

            model.UpdateConfiguration(request.Configuration);
            await _modelRepository.UpdateModelMetadataAsync(model);

            await _publisher.Publish(new ModelUpdatedEvent(model.Id, modelName));
            return true;
        }

        public async Task<List<ModelVersion>> GetModelVersionsAsync(string modelName)
        {
            return await _modelRepository.GetModelVersionsAsync(modelName);
        }

        public async Task<List<ModelMetadata>> GetAllModelsAsync()
        {
            try
            {
                _logger.LogInformation("Tüm modeller getiriliyor");
                
                var models = await _modelRepository.GetAllModelsAsync();
                
                _logger.LogInformation("Toplam {Count} model bulundu", models.Count);
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm modeller getirilirken hata oluştu");
                throw;
            }
        }

        public async Task<bool> UpdateModelStatusAsync(Guid modelId, string status)
        {
            try
            {
                _logger.LogInformation("Model status güncelleniyor: {ModelId}, Yeni Status: {Status}", modelId, status);

                // Önce modeli bulalım (ID ile)
                var models = await _modelRepository.GetAllModelsAsync();
                var model = models.FirstOrDefault(m => m.Id == modelId);

                if (model == null)
                {
                    _logger.LogWarning("Model bulunamadı: {ModelId}", modelId);
                    return false;
                }

                // AKTIF MODEL KONTROLÜ: Aynı tipten başka aktif model var mı kontrol et
                if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
                {
                    var existingActiveModel = models.FirstOrDefault(m => 
                        m.Type == model.Type && 
                        m.Status == ModelStatus.Active && 
                        m.Id != modelId);

                    if (existingActiveModel != null)
                    {
                        _logger.LogWarning("❌ Aktif model kontrolü başarısız! {ModelType} tipinde zaten aktif model var: {ExistingModelId} - {ExistingModelName}", 
                            model.Type, existingActiveModel.Id, existingActiveModel.ModelName);
                        
                        throw new InvalidOperationException(
                            $"❌ {model.Type} tipinde zaten aktif bir model bulunuyor: {existingActiveModel.ModelName} (v{existingActiveModel.Version}). " +
                            $"Aynı anda sadece bir model aktif olabilir. Önce mevcut aktif modeli pasif yapınız.");
                    }
                    
                    _logger.LogInformation("✅ Aktif model kontrolü başarılı - {ModelType} tipinde başka aktif model yok", model.Type);
                }

                // Status validate et ve güncelle
                bool success = false;
                if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
                {
                    success = UpdateStatusToActive(model);
                }
                else if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
                {
                    success = UpdateStatusToInactive(model);
                }
                else if (string.Equals(status, "training", StringComparison.OrdinalIgnoreCase))
                {
                    success = UpdateStatusToTraining(model);
                }
                else if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    success = UpdateStatusToFailed(model);
                }

                if (!success)
                {
                    _logger.LogWarning("Geçersiz status: {Status}", status);
                    return false;
                }

                // Repository'ye kaydet
                await _modelRepository.UpdateModelMetadataAsync(model);

                // Domain event publish et
                await _publisher.Publish(new ModelStatusUpdatedEvent(model.Id, model.ModelName, status));

                _logger.LogInformation("✅ Model status başarıyla güncellendi: {ModelId}, Yeni Status: {Status}", 
                    modelId, status);

                return true;
            }
            catch (InvalidOperationException)
            {
                // Aktif model kontrol hatası - re-throw
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model status güncelleme sırasında hata: {ModelId}, {Status}", modelId, status);
                throw;
            }
        }

        private bool UpdateStatusToActive(ModelMetadata model)
        {
            model.Activate();
            return true;
        }

        private bool UpdateStatusToInactive(ModelMetadata model)
        {
            model.Deactivate();
            return true;
        }

        private bool UpdateStatusToTraining(ModelMetadata model)
        {
            model.StartTraining();
            return true;
        }

        private bool UpdateStatusToFailed(ModelMetadata model)
        {
            model.MarkAsFailed("Manual status update");
            return true;
        }

        public async Task<bool> ActivateModelVersionAsync(string modelName, string version)
        {
            var model = await _modelRepository.GetModelAsync(modelName, version);
            if (model == null) return false;

            var activeModel = await _modelRepository.GetActiveModelAsync(modelName);
            if (activeModel != null)
            {
                activeModel.Deactivate();
                await _modelRepository.UpdateModelMetadataAsync(activeModel);
            }

            model.Activate();
            await _modelRepository.UpdateModelMetadataAsync(model);

            await _publisher.Publish(new ModelActivatedEvent(model.Id, modelName, version));
            return true;
        }

        private async Task<string> EnsureDatasetExists(ModelType modelType)
        {
            var defaultCsvPath = Path.Combine(_dataPath, "creditcard.csv");

            if (File.Exists(defaultCsvPath))
            {
                _logger.LogInformation("Mevcut veri seti kullanılıyor: {Path}", defaultCsvPath);
                return defaultCsvPath;
            }

            _logger.LogWarning("Veri seti bulunamadı, özel bir yol kullanabilirsiniz");
            return defaultCsvPath;
        }

        private string DetermineModelTypeFromEnum(ModelType modelType)
        {
            return modelType switch
            {
                ModelType.LightGBM => "lightgbm",
                ModelType.PCA => "pca",
                ModelType.Ensemble => "ensemble",
                _ => "ensemble"
            };
        }

        private string GenerateVersion()
        {
            return $"v{DateTime.UtcNow:yyyyMMdd.HHmmss}";
        }
        // ModelService.cs'ye eklenecek tek method

#region Advanced ML Extension - Minimal

/// <summary>
/// Advanced model ile tahmin - mevcut yapıyla uyumlu
/// </summary>
public async Task<ModelPrediction> PredictAdvancedAsync(ModelInput input, string advancedModelType)
{
    try
    {
        _logger.LogInformation("Advanced model ile tahmin: {ModelType}", advancedModelType);

        // Advanced model info path'ini bul
        var modelInfoPath = await FindAdvancedModelInfoPath(advancedModelType);
        
        if (string.IsNullOrEmpty(modelInfoPath))
        {
            _logger.LogWarning("Advanced model bulunamadı: {ModelType}, fallback kullanılıyor", advancedModelType);
            return CreateAdvancedAlternativePrediction(input, advancedModelType, "Model not found");
        }

        try
        {
            // Python service ile tahmin yap
            var prediction = await _pythonService.PredictAdvancedAsync(input, modelInfoPath, advancedModelType);
            
            // Metadata'ya advanced bilgiler ekle
            if (prediction.Metadata == null)
                prediction.Metadata = new Dictionary<string, object>();

            prediction.Metadata["IsAdvancedModel"] = true;
            prediction.Metadata["AdvancedModelType"] = advancedModelType;
            prediction.Metadata["UsedAt"] = DateTime.UtcNow;

            _logger.LogInformation("Advanced tahmin başarılı: {ModelType}, Probability: {Probability:F4}",
                advancedModelType, prediction.Probability);

            return prediction;
        }
        catch (Exception pythonEx)
        {
            _logger.LogError(pythonEx, "Advanced Python entegrasyonu hatası: {ModelType}", advancedModelType);
            return CreateAdvancedAlternativePrediction(input, advancedModelType, 
                $"Python integration error: {pythonEx.Message}");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Advanced tahmin genel hatası: {ModelType}", advancedModelType);
        return CreateAdvancedAlternativePrediction(input, advancedModelType, 
            $"General prediction error: {ex.Message}");
    }
}

private async Task<string> FindAdvancedModelInfoPath(string modelType)
{
    try
    {
        var modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Models");
        
        // Advanced model dizinlerini ara
        var modelDirs = Directory.GetDirectories(modelsPath, $"advanced_{modelType}_*");
        
        if (!modelDirs.Any())
        {
            _logger.LogWarning("Advanced model dizini bulunamadı: {ModelType}", modelType);
            return null;
        }

        // En son oluşturulan dizini al
        var latestDir = modelDirs.OrderByDescending(d => d).First();
        
        // Model info dosyasını bul
        var infoFiles = Directory.GetFiles(latestDir, "model_info_*.json");
        
        if (!infoFiles.Any())
        {
            _logger.LogWarning("Advanced model info dosyası bulunamadı: {ModelType}", modelType);
            return null;
        }

        return infoFiles.First();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Advanced model info path aranırken hata: {ModelType}", modelType);
        return null;
    }
}

private ModelPrediction CreateAdvancedAlternativePrediction(ModelInput input, string modelType, string reason)
{
    try
    {
        _logger.LogInformation("Advanced alternative prediction: {ModelType}", modelType);

        // Business threshold
        var businessThreshold = modelType.ToLower() switch
        {
            "attention" => 0.18,
            "autoencoder" => 0.12,
            "isolation_forest" => 0.10,
            "gan_augmented" => 0.14,
            _ => 0.15
        };

        // Enhanced rule-based prediction
        double probability = 0.08;

        // V-risk calculation (geliştirilmiş)
        var vRiskScore = CalculateAdvancedVRiskScore(input);
        if (vRiskScore > 0.3)
        {
            probability += vRiskScore * 0.45;
        }

        // Amount risk (geliştirilmiş)
        var normalizedAmount = Math.Min(1.0, input.Amount);
        if (normalizedAmount > 0.8) probability += 0.4;
        else if (normalizedAmount > 0.6) probability += 0.25;
        else if (normalizedAmount > 0.4) probability += 0.15;

        // Time risk (geliştirilmiş)
        var hourOfDay = ((int)(input.Time / 3600)) % 24;
        if (hourOfDay < 5 || hourOfDay > 23) probability += 0.18;
        else if (hourOfDay < 7 || hourOfDay > 22) probability += 0.12;

        // Model-specific adjustments
        probability = modelType.ToLower() switch
        {
            "attention" => probability * 1.1,      // Attention daha hassas
            "autoencoder" => probability * 0.95,   // AutoEncoder daha konservatif
            "isolation_forest" => probability * 1.0,
            "gan_augmented" => probability * 1.05,
            _ => probability
        };

        probability = Math.Max(0.02, Math.Min(0.88, probability));

        var isHighRisk = probability >= businessThreshold;
        var confidence = CalculateAdvancedConfidence(probability, modelType);

        return new ModelPrediction
        {
            PredictedLabel = isHighRisk,
            Probability = probability,
            Score = probability,
            AnomalyScore = probability * 2.2,
            ModelType = $"Advanced_{modelType}_Alternative",
            ErrorMessage = $"Advanced alternative: {reason}",
            PredictionTime = DateTime.UtcNow,
            Confidence = confidence,
            
            Metadata = new Dictionary<string, object>
            {
                ["IsAdvancedAlternative"] = true,
                ["AdvancedModelType"] = modelType,
                ["BusinessThreshold"] = businessThreshold,
                ["ErrorMessage"] = reason,
                ["FallbackMethod"] = "Advanced_Business_Rule_Based",
                ["VRiskScore"] = vRiskScore,
                ["AmountRisk"] = normalizedAmount,
                ["TimeRisk"] = (hourOfDay < 6 || hourOfDay > 22) ? "High" : "Normal",
                ["Timestamp"] = DateTime.UtcNow
            }
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Advanced alternative prediction oluşturulurken hata");
        
        return new ModelPrediction
        {
            PredictedLabel = false,
            Probability = 0.2,
            Score = 0.2,
            AnomalyScore = 0.4,
            ModelType = $"Advanced_{modelType}_CriticalFallback",
            ErrorMessage = $"Critical fallback: {ex.Message}",
            PredictionTime = DateTime.UtcNow,
            Confidence = 0.3,
            
            Metadata = new Dictionary<string, object>
            {
                ["IsCriticalFallback"] = true,
                ["OriginalError"] = reason,
                ["FallbackError"] = ex.Message
            }
        };
    }
}

private double CalculateAdvancedVRiskScore(ModelInput input)
{
    var riskScore = 0.0;
    var riskCount = 0;

    // Geliştirilmiş V-feature risk kontrolü
    var riskChecks = new (string property, double threshold, double weight)[]
    {
        (nameof(input.V1), -2.5, 1.2),
        (nameof(input.V2), 2.5, 1.0),
        (nameof(input.V3), -3.5, 1.3),
        (nameof(input.V4), -1.5, 0.9),
        (nameof(input.V7), -5.0, 1.4),
        (nameof(input.V10), -3.5, 1.3),
        (nameof(input.V11), 2.0, 1.1),
        (nameof(input.V12), -3.0, 1.2),
        (nameof(input.V14), -4.5, 1.5),   // En yüksek ağırlık
        (nameof(input.V16), -2.0, 1.0),
        (nameof(input.V17), -4.0, 1.3),
        (nameof(input.V18), -2.5, 1.1)
    };

    foreach (var (property, threshold, weight) in riskChecks)
    {
        var value = property switch
        {
            nameof(input.V1) => input.V1,
            nameof(input.V2) => input.V2,
            nameof(input.V3) => input.V3,
            nameof(input.V4) => input.V4,
            nameof(input.V7) => input.V7,
            nameof(input.V10) => input.V10,
            nameof(input.V11) => input.V11,
            nameof(input.V12) => input.V12,
            nameof(input.V14) => input.V14,
            nameof(input.V16) => input.V16,
            nameof(input.V17) => input.V17,
            nameof(input.V18) => input.V18,
            _ => 0
        };

        if ((threshold < 0 && value < threshold) || (threshold > 0 && value > threshold))
        {
            riskScore += weight;
            riskCount++;
        }
    }

    var normalizedScore = riskScore / riskChecks.Sum(x => x.weight);
    
    // Çoklu extreme değerler için bonus
    if (riskCount >= 4) normalizedScore *= 1.5;
    else if (riskCount >= 3) normalizedScore *= 1.3;
    else if (riskCount >= 2) normalizedScore *= 1.15;

    return Math.Min(1.0, normalizedScore);
}

private double CalculateAdvancedConfidence(double probability, string modelType)
{
    var baseConfidence = modelType.ToLower() switch
    {
        "attention" => 0.85,
        "autoencoder" => 0.75,
        "isolation_forest" => 0.70,
        "gan_augmented" => 0.80,
        _ => 0.65
    };

    // Probability extremity bonus
    var extremityBonus = Math.Abs(probability - 0.5) * 0.3;
    
    var finalConfidence = baseConfidence + extremityBonus;
    return Math.Max(0.4, Math.Min(0.95, finalConfidence));
}

#endregion
    }
}