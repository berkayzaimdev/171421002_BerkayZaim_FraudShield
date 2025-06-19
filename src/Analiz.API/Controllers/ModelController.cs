using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Analiz.Application.DTOs.ML;
using Analiz.Application.DTOs.Request;
using Analiz.Application.Interfaces;
using Analiz.Application.Models;
using Analiz.Application.Services;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Analiz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModelController : ControllerBase
    {
        private readonly IModelService _modelService;
        private readonly IFraudDetectionService _FraudDetectionService;
        private readonly PythonMLIntegrationService _pythonService;
        private readonly ILogger<ModelController> _logger;

        public ModelController(IModelService modelService, IFraudDetectionService detectionService,PythonMLIntegrationService pythonService,ILogger<ModelController> logger)
        {
            _modelService = modelService;
            _FraudDetectionService = detectionService;
            _pythonService = pythonService;
            _logger = logger;
        }

        /// <summary>
        /// LightGBM modeli eğit (Geliştirilmiş metriklerle)
        /// </summary>
        [HttpPost("train/lightgbm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TrainLightGBM()
        {
            try
            {
                var config = new LightGBMConfigurationDTO();
                _logger.LogInformation("LightGBM model eğitimi başlatılıyor");
    
                var configJson = JsonSerializer.Serialize(new { lightgbm = config });

                var request = new TrainingRequest
                {
                    ModelName = $"CreditCard_FraudDetection_LightGBM_{DateTime.Now:yyyyMMdd}",
                    ModelType = ModelType.LightGBM,
                    Configuration = configJson
                };

                var result = await _modelService.TrainModelAsync(request);

                return Ok(CreateEnhancedTrainingResponse(result, request.ModelName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LightGBM model eğitimi başarısız oldu");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// LightGBM modeli eğit (Konfigürasyonlu - Geliştirilmiş metriklerle)
        /// </summary>
        [HttpPost("train/lightgbm-config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TrainLightGBMConfig([FromBody] LightGBMConfigurationDTO config)
        {
            try
            {
                _logger.LogInformation("LightGBM model eğitimi başlatılıyor (Konfigürasyonlu)");
    
                var configJson = JsonSerializer.Serialize(new { lightgbm = config });

                var request = new TrainingRequest
                {
                    ModelName = $"CreditCard_FraudDetection_LightGBM_{DateTime.Now:yyyyMMdd}",
                    ModelType = ModelType.LightGBM,
                    Configuration = configJson
                };

                var result = await _modelService.TrainModelAsync(request);

                return Ok(CreateEnhancedTrainingResponse(result, request.ModelName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LightGBM model eğitimi başarısız oldu");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// PCA modeli eğit (Geliştirilmiş metriklerle)
        /// </summary>
        [HttpPost("train/pca")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TrainPCA()
        {
            try
            {
                var config = new PCAConfigurationDTO();
                _logger.LogInformation("PCA model eğitimi başlatılıyor");

                var configJson = JsonSerializer.Serialize(new { pca = config });

                var request = new TrainingRequest
                {
                    ModelName = $"CreditCard_AnomalyDetection_PCA_{DateTime.Now:yyyyMMdd}",
                    ModelType = ModelType.PCA,
                    Configuration = configJson
                };

                var result = await _modelService.TrainModelAsync(request);

                return Ok(CreateEnhancedTrainingResponse(result, request.ModelName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PCA model eğitimi başarısız oldu");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// PCA modeli eğit (Konfigürasyonlu - Geliştirilmiş metriklerle)
        /// </summary>
        [HttpPost("train/pca-config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TrainPCAConfig([FromBody] PCAConfigurationDTO config)
        {
            try
            {
                _logger.LogInformation("PCA model eğitimi başlatılıyor (Konfigürasyonlu)");

                var configJson = JsonSerializer.Serialize(new { pca = config });

                var request = new TrainingRequest
                {
                    ModelName = $"CreditCard_AnomalyDetection_PCA_{DateTime.Now:yyyyMMdd}",
                    ModelType = ModelType.PCA,
                    Configuration = configJson
                };

                var result = await _modelService.TrainModelAsync(request);

                return Ok(CreateEnhancedTrainingResponse(result, request.ModelName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PCA model eğitimi başarısız oldu");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Ensemble modeli eğit (Geliştirilmiş metriklerle)
        /// </summary>
        [HttpPost("train/ensemble")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TrainEnsemble()
        {
            try
            {
                var config = new EnsembleConfigurationDTO();
                _logger.LogInformation("Ensemble model eğitimi başlatılıyor");

                var configJson = JsonSerializer.Serialize(config);

                var request = new TrainingRequest
                {
                    ModelName = $"CreditCard_Ensemble_{DateTime.Now:yyyyMMdd}",
                    ModelType = ModelType.Ensemble,
                    Configuration = configJson
                };

                var result = await _modelService.TrainEnsembleModelAsync(request);

                return Ok(CreateEnhancedTrainingResponse(result, request.ModelName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ensemble model eğitimi başarısız oldu");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Ensemble modeli eğit (Konfigürasyonlu - Geliştirilmiş metriklerle)
        /// </summary>
        [HttpPost("train/ensemble-config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TrainEnsembleConfig([FromBody] EnsembleConfigurationDTO config)
        {
            try
            {
                _logger.LogInformation("Ensemble model eğitimi başlatılıyor (Konfigürasyonlu)");

                var configJson = JsonSerializer.Serialize(config);

                var request = new TrainingRequest
                {
                    ModelName = $"CreditCard_Ensemble_{DateTime.Now:yyyyMMdd}",
                    ModelType = ModelType.Ensemble,
                    Configuration = configJson
                };

                var result = await _modelService.TrainEnsembleModelAsync(request);

                return Ok(CreateEnhancedTrainingResponse(result, request.ModelName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ensemble model eğitimi başarısız oldu");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Model metrikleri (Geliştirilmiş)
        /// </summary>
        [HttpGet("{modelName}/metrics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetModelMetrics(string modelName)
        {
            try
            {
                var metrics = await _modelService.GetModelMetricsAsync(modelName);

                return Ok(new
                {
                    ModelName = modelName,
                    BasicMetrics = new
                    {
                        metrics.Accuracy,
                        metrics.Precision,
                        metrics.Recall,
                        metrics.F1Score,
                        metrics.AUC,
                        metrics.AUCPR
                    },
                    ConfusionMatrix = new
                    {
                        metrics.TruePositive,
                        metrics.TrueNegative,
                        metrics.FalsePositive,
                        metrics.FalseNegative,
                        Matrix = metrics.GetConfusionMatrix()
                    },
                    ExtendedMetrics = new
                    {
                        metrics.Specificity,
                        metrics.Sensitivity,
                        metrics.NPV,
                        metrics.FPR,
                        metrics.FNR,
                        metrics.FDR,
                        metrics.FOR,
                        metrics.BalancedAccuracy,
                        metrics.MatthewsCorrCoef,
                        metrics.CohenKappa,
                        metrics.OptimalThreshold
                    },
                    ProbabilisticMetrics = new
                    {
                        metrics.LogLoss,
                        metrics.BrierScore
                    },
                    ClassDistribution = new
                    {
                        Class0Support = metrics.SupportClass0,
                        Class1Support = metrics.SupportClass1,
                        metrics.ClassImbalanceRatio
                    },
                    AnomalyDetection = new
                    {
                        metrics.AnomalyThreshold,
                        metrics.MeanReconstructionError,
                        metrics.StdReconstructionError,
                        metrics.ExplainedVarianceRatio
                    },
                    ClassificationReport = metrics.ClassificationReport,
                    PerformanceSummary = metrics.GetPerformanceSummary(),
                    AllMetrics = metrics.ToDictionary()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model metrikleri alınırken hata: {ModelName}", modelName);
                return NotFound(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Model performans özeti
        /// </summary>
        [HttpGet("{modelName}/performance-summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetModelPerformanceSummary(string modelName)
        {
            try
            {
                var metrics = await _modelService.GetModelMetricsAsync(modelName);
                var summary = metrics.GetPerformanceSummary();

                return Ok(new
                {
                    ModelName = modelName,
                    Summary = summary,
                    KeyMetrics = new
                    {
                        metrics.Accuracy,
                        metrics.F1Score,
                        metrics.AUC,
                        metrics.BalancedAccuracy
                    },
                    RiskAssessment = new
                    {
                        FraudDetectionRate = metrics.Recall,
                        FalseAlarmRate = metrics.FPR,
                        MissedFraudRate = metrics.FNR,
                        TrustScore = 1 - metrics.FPR // Güven skoru
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model performans özeti alınırken hata: {ModelName}", modelName);
                return NotFound(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Model karşılaştırması
        /// </summary>
        [HttpPost("compare")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompareModels([FromBody] string[] modelNames)
        {
            try
            {
                if (modelNames == null || modelNames.Length < 2)
                {
                    return BadRequest(new { Error = "En az 2 model adı gerekli" });
                }

                var comparisons = new List<object>();

                foreach (var modelName in modelNames)
                {
                    try
                    {
                        var metrics = await _modelService.GetModelMetricsAsync(modelName);
                        var summary = metrics.GetPerformanceSummary();

                        comparisons.Add(new
                        {
                            ModelName = modelName,
                            OverallScore = summary.OverallScore,
                            Grade = summary.OverallScore > 0.9 ? "A+" :
                                   summary.OverallScore > 0.8 ? "A" :
                                   summary.OverallScore > 0.7 ? "B" :
                                   summary.OverallScore > 0.6 ? "C" : "D",
                            KeyMetrics = new
                            {
                                metrics.Accuracy,
                                metrics.Precision,
                                metrics.Recall,
                                metrics.F1Score,
                                metrics.AUC,
                                metrics.BalancedAccuracy
                            },
                            Strengths = GetModelStrengths(metrics),
                            Weaknesses = GetModelWeaknesses(metrics)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Model metrikleri alınamadı: {ModelName}", modelName);
                        
                        comparisons.Add(new
                        {
                            ModelName = modelName,
                            Error = ex.Message,
                            Available = false
                        });
                    }
                }

                // En iyi modeli belirle
                var bestModel = comparisons
                    .Where(c => c.GetType().GetProperty("OverallScore") != null)
                    .OrderByDescending(c => (double)c.GetType().GetProperty("OverallScore").GetValue(c))
                    .FirstOrDefault();

                return Ok(new
                {
                    ComparedModels = comparisons,
                    BestModel = bestModel?.GetType().GetProperty("ModelName")?.GetValue(bestModel),
                    Recommendation = GenerateModelRecommendation(comparisons)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model karşılaştırması sırasında hata");
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Diğer existing metodlar (versiyonlar, aktivasyon, tahmin vb.)
        [HttpGet("{modelName}/versions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetModelVersions(string modelName)
        {
            try
            {
                var versions = await _modelService.GetModelVersionsAsync(modelName);

                if (versions == null || !versions.Any())
                {
                    return NotFound(new { Error = $"Model bulunamadı: {modelName}" });
                }

                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model versiyonları alınırken hata: {ModelName}", modelName);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{modelName}/versions/{version}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateModelVersion(string modelName, string version)
        {
            try
            {
                var success = await _modelService.ActivateModelVersionAsync(modelName, version);

                if (!success)
                {
                    return NotFound(new { Error = $"Model bulunamadı: {modelName}, Versiyon: {version}" });
                }

                return Ok(new { Success = true, Message = $"Model aktifleştirildi: {modelName}, Versiyon: {version}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model aktivasyonu sırasında hata: {ModelName}, {Version}", modelName, version);
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Model durumunu güncelle (Training, Active, Inactive, Failed)
        /// </summary>
        [HttpPut("{modelId}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateModelStatus(Guid modelId, [FromBody] UpdateModelStatusRequest request)
        {
            try
            {
                _logger.LogInformation("Model status güncelleniyor: {ModelId}, Yeni Status: {Status}", modelId, request.Status);

                var success = await _modelService.UpdateModelStatusAsync(modelId, request.Status);

                if (!success)
                {
                    return NotFound(new { Error = $"Model bulunamadı: {modelId}" });
                }

                return Ok(new { 
                    Success = true, 
                    Message = $"Model durumu başarıyla {request.Status} olarak güncellendi",
                    ModelId = modelId,
                    NewStatus = request.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model status güncelleme sırasında hata: {ModelId}, {Status}", modelId, request.Status);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("predict")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Predict([FromBody] TransactionRequest transaction)
        {
            try
            {
                var prediction = await _FraudDetectionService.AnalyzeTransactionAsync(transaction);

                return Ok(new
                {
                    TransactionId = prediction.TransactionId,
                    IsFraudulent = prediction.Decision,
                    Probability = prediction.FraudProbability,
                    Score = prediction.RiskScore,
                    AnomalyScore = prediction.AnomalyScore,
                    RiskLevel=prediction.RiskScore.Level
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tahmin sırasında hata");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{modelType}/predict")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PredictWithModel(ModelType type, [FromBody] TransactionData transaction)
        {
            try
            {
                var input = ModelInput.FromTransactionData(transaction);
                string modelName;
                if (type == ModelType.PCA)
                    modelName = "CreditCard_FraudDetection_PCA";
                else if(type==ModelType.LightGBM)
                    
                    modelName="CreditCard_FraudDetection_LightGBM";
                else
                    modelName="CreditCard_FraudDetection_Ensemble";
                var prediction = await _modelService.PredictAsync(modelName, input,type);

                return Ok(new
                {
                    TransactionId = transaction.TransactionId,
                    IsFraudulent = prediction.PredictedLabel,
                    Probability = prediction.Probability,
                    Score = prediction.Score,
                    AnomalyScore = prediction.AnomalyScore,
                    Metadata = prediction.Metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tahmin sırasında hata: {type}", type);

                if (ex.Message.Contains("bulunamadı"))
                {
                    return NotFound(new { Error = ex.Message });
                }

                return BadRequest(new { Error = ex.Message });
            }
        }

        // Yardımcı metodlar
        private object CreateEnhancedTrainingResponse(TrainingResult result, string modelName)
        {
            var summary = result.Metrics.GetPerformanceSummary();
            
            return new
            {
                ModelId = result.ModelId,
                ModelName = modelName,
                BasicMetrics = new
                {
                    result.Metrics.Accuracy,
                    result.Metrics.Precision,
                    result.Metrics.Recall,
                    result.Metrics.F1Score,
                    result.Metrics.AUC,
                    result.Metrics.AUCPR
                },
                ConfusionMatrix = new
                {
                    result.Metrics.TruePositive,
                    result.Metrics.TrueNegative,
                    result.Metrics.FalsePositive,
                    result.Metrics.FalseNegative
                },
                ExtendedMetrics = new
                {
                    result.Metrics.Specificity,
                    result.Metrics.Sensitivity,
                    result.Metrics.BalancedAccuracy,
                    result.Metrics.MatthewsCorrCoef
                },
                PerformanceSummary = new
                {
                    summary.OverallScore,
                    summary.IsGoodModel,
                    summary.PrimaryWeakness,
                    ModelGrade = summary.OverallScore > 0.9 ? "A+" :
                                summary.OverallScore > 0.8 ? "A" :
                                summary.OverallScore > 0.7 ? "B" :
                                summary.OverallScore > 0.6 ? "C" : "D"
                },
                TrainingTime = result.TrainingTime.TotalSeconds,
                Recommendations = summary.RecommendedActions
            };
        }

        private List<string> GetModelStrengths(ModelMetrics metrics)
        {
            var strengths = new List<string>();
            
            if (metrics.Accuracy > 0.9) strengths.Add("Çok yüksek doğruluk");
            if (metrics.Precision > 0.8) strengths.Add("Düşük yanlış alarm");
            if (metrics.Recall > 0.8) strengths.Add("Yüksek fraud yakalama");
            if (metrics.AUC > 0.9) strengths.Add("Mükemmel ayırt etme");
            if (metrics.BalancedAccuracy > 0.85) strengths.Add("Dengeli performans");
            
            return strengths;
        }

        private List<string> GetModelWeaknesses(ModelMetrics metrics)
        {
            var weaknesses = new List<string>();
            
            if (metrics.Precision < 0.7) weaknesses.Add("Yüksek yanlış alarm");
            if (metrics.Recall < 0.7) weaknesses.Add("Düşük fraud yakalama");
            if (metrics.Specificity < 0.8) weaknesses.Add("Normal işlem tanıma sorunu");
            if (metrics.ClassImbalanceRatio > 100) weaknesses.Add("Sınıf dengesizliği");
            
            return weaknesses;
        }

        private string GenerateModelRecommendation(List<object> comparisons)
        {
            var validComparisons = comparisons.Count(c => c.GetType().GetProperty("OverallScore") != null);
            
            if (validComparisons == 0)
                return "Karşılaştırılabilir model bulunamadı";
            
            if (validComparisons == 1)
                return "Tek model mevcut, karşılaştırma için daha fazla model eğitin";
            
            return "Ensemble modelini production'da, LightGBM'i hızlı tahmin için kullanın";
        }
        // ModelController.cs'ye eklenecek metodlar

#region Advanced ML Endpoints - Minimal Extension

/// <summary>
/// Attention model eğit
/// </summary>
[HttpPost("train/attention")]
public async Task<IActionResult> TrainAttentionModel([FromBody] object config = null)
{
    try
    {
        var configJson = config != null ? JsonSerializer.Serialize(new { attention = config }) : 
                        JsonSerializer.Serialize(new { attention = new { 
                            hidden_dim = 128, num_heads = 8, num_layers = 4, epochs = 100 
                        }});

        var request = new TrainingRequest
        {
            ModelName = $"CreditCard_Attention_{DateTime.Now:yyyyMMdd}",
            ModelType = ModelType.LightGBM, // Enum'da yeni tip eklemeye gerek yok
            Configuration = configJson
        };

        // Mevcut Python service'i kullan, sadece model type'ı değiştir
        var (success, modelPath, metrics) = await _pythonService.TrainAdvancedModelAsync(
            "attention", configJson, GetDefaultDataPath());

        if (!success)
            return BadRequest(new { Error = "Attention model eğitimi başarısız" });

        return Ok(new
        {
            ModelName = request.ModelName,
            ModelType = "attention",
            Success = success,
            ModelPath = modelPath,
            Metrics = metrics,
            Message = "Attention model başarıyla eğitildi"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Attention model eğitimi hatası");
        return BadRequest(new { Error = ex.Message });
    }
}

/// <summary>
/// AutoEncoder model eğit
/// </summary>
[HttpPost("train/autoencoder")]
public async Task<IActionResult> TrainAutoEncoder([FromBody] object config = null)
{
    try
    {
        var configJson = config != null ? JsonSerializer.Serialize(new { autoencoder = config }) : 
                        JsonSerializer.Serialize(new { autoencoder = new { 
                            hidden_dims = new[] { 64, 32, 16 }, epochs = 200, contamination = 0.1 
                        }});

        var (success, modelPath, metrics) = await _pythonService.TrainAdvancedModelAsync(
            "autoencoder", configJson, GetDefaultDataPath());

        if (!success)
            return BadRequest(new { Error = "AutoEncoder model eğitimi başarısız" });

        return Ok(new
        {
            ModelType = "autoencoder",
            Success = success,
            ModelPath = modelPath,
            Metrics = metrics,
            Message = "AutoEncoder model başarıyla eğitildi"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "AutoEncoder model eğitimi hatası");
        return BadRequest(new { Error = ex.Message });
    }
}

/// <summary>
/// Isolation Forest model eğit
/// </summary>
[HttpPost("train/isolation-forest")]
public async Task<IActionResult> TrainIsolationForest([FromBody] object config = null)
{
    try
    {
        var configJson = config != null ? JsonSerializer.Serialize(new { isolation_forest = config }) : 
                        JsonSerializer.Serialize(new { isolation_forest = new { 
                            n_estimators = 100, contamination = 0.1 
                        }});

        var (success, modelPath, metrics) = await _pythonService.TrainAdvancedModelAsync(
            "isolation_forest", configJson, GetDefaultDataPath());

        if (!success)
            return BadRequest(new { Error = "Isolation Forest model eğitimi başarısız" });

        return Ok(new
        {
            ModelType = "isolation_forest",
            Success = success,
            ModelPath = modelPath,
            Metrics = metrics,
            Message = "Isolation Forest model başarıyla eğitildi"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Isolation Forest model eğitimi hatası");
        return BadRequest(new { Error = ex.Message });
    }
}

/// <summary>
/// SMOTE ile veri dengeleme + model eğitimi
/// </summary>
[HttpPost("train/{modelType}/with-smote")]
public async Task<IActionResult> TrainWithSMOTE(string modelType, [FromBody] object config = null)
{
    try
    {
        var configJson = config != null ? JsonSerializer.Serialize(config) : "{}";

        var (success, modelPath, metrics) = await _pythonService.TrainWithDataBalancingAsync(
            modelType, configJson, GetDefaultDataPath(), "smote");

        if (!success)
            return BadRequest(new { Error = $"SMOTE ile {modelType} model eğitimi başarısız" });

        return Ok(new
        {
            ModelType = $"{modelType}_smote",
            Success = success,
            ModelPath = modelPath,
            Metrics = metrics,
            Message = $"SMOTE ile {modelType} model başarıyla eğitildi"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "SMOTE ile model eğitimi hatası: {ModelType}", modelType);
        return BadRequest(new { Error = ex.Message });
    }
}

/// <summary>
/// Advanced model ile tahmin
/// </summary>
[HttpPost("predict/advanced/{modelType}")]
public async Task<IActionResult> PredictWithAdvanced(string modelType, [FromBody] TransactionData transaction)
{
    try
    {
        var input = ModelInput.FromTransactionData(transaction);
        
        // Mevcut prediction service'i kullan, sadece model type'ı farklı
        var prediction = await _modelService.PredictAdvancedAsync(input, modelType);

        return Ok(new
        {
            TransactionId = transaction.TransactionId,
            IsFraudulent = prediction.PredictedLabel,
            Probability = prediction.Probability,
            Score = prediction.Score,
            AnomalyScore = prediction.AnomalyScore,
            ModelType = modelType,
            Confidence = prediction.Confidence,
            IsAdvancedModel = true
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Advanced model tahmin hatası: {ModelType}", modelType);
        return BadRequest(new { Error = ex.Message });
    }
}

/// <summary>
/// Basit model karşılaştırması
/// </summary>
[HttpPost("compare-advanced")]
public async Task<IActionResult> CompareAdvancedModels([FromBody] string[] modelTypes)
{
    try
    {
        var results = new List<object>();
        
        foreach (var modelType in modelTypes)
        {
            try
            {
                // Basit config ile model eğit
                var config = "{}";
                var (success, modelPath, metrics) = await _pythonService.TrainAdvancedModelAsync(
                    modelType, config, GetDefaultDataPath());

                if (success && metrics != null)
                {
                    results.Add(new
                    {
                        ModelType = modelType,
                        Success = true,
                        Accuracy = metrics.GetValueOrDefault("accuracy", 0),
                        F1Score = metrics.GetValueOrDefault("f1_score", 0),
                        AUC = metrics.GetValueOrDefault("auc", 0),
                        OverallScore = (metrics.GetValueOrDefault("accuracy", 0) + 
                                      metrics.GetValueOrDefault("f1_score", 0) + 
                                      metrics.GetValueOrDefault("auc", 0)) / 3
                    });
                }
                else
                {
                    results.Add(new { ModelType = modelType, Success = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Model karşılaştırma hatası: {ModelType}", modelType);
                results.Add(new { ModelType = modelType, Success = false, Error = ex.Message });
            }
        }

        var bestModel = results
            .Where(r => (bool)r.GetType().GetProperty("Success").GetValue(r))
            .OrderByDescending(r => (double)r.GetType().GetProperty("OverallScore").GetValue(r))
            .FirstOrDefault();

        return Ok(new
        {
            Results = results,
            BestModel = bestModel?.GetType().GetProperty("ModelType").GetValue(bestModel),
            Recommendation = "En yüksek overall score'a sahip model production için önerilir"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Advanced model karşılaştırması hatası");
        return BadRequest(new { Error = ex.Message });
    }
}
private string GetDefaultDataPath()
{
    // PythonMLIntegrationService'den data path'i al
    return _pythonService.GetDataPath();
}

#endregion

        /// <summary>
        /// Tüm modelleri getir
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllModels()
        {
            try
            {
                _logger.LogInformation("Tüm modeller getiriliyor");
                
                var models = await _modelService.GetAllModelsAsync();
                
                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm modeller getirilirken hata oluştu");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Model metriklerini al (Geliştirilmiş)
        /// </summary>
    }
}