using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using Analiz.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<TransactionController> _logger;
    private readonly IAnalysisResultRepository _analysisResultRepository;
    private readonly IRiskEvaluationRepository _riskEvaluationRepository;
    private readonly IRiskFactorRepository _riskFactorRepository;

    public TransactionController(
        ITransactionRepository transactionRepository,
        IFraudDetectionService fraudDetectionService,
        ILogger<TransactionController> logger,
        IAnalysisResultRepository analysisResultRepository,
        IRiskEvaluationRepository riskEvaluationRepository,
        IRiskFactorRepository riskFactorRepository)
    {
        _transactionRepository = transactionRepository;
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
        _analysisResultRepository = analysisResultRepository;
        _riskEvaluationRepository = riskEvaluationRepository;
        _riskFactorRepository = riskFactorRepository;
    }

    /// <summary>
    /// Tüm işlemleri listeler (sayfalama ile)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation("İşlemler listeleniyor: limit={Limit}, offset={Offset}", limit, offset);

            List<Transaction> transactions;

            if (startDate.HasValue && endDate.HasValue)
            {
                transactions = await _transactionRepository.GetTransactionsBetweenDatesAsync(
                    startDate.Value, endDate.Value);
            }
            else
            {
                // Varsayılan olarak son 30 günün işlemlerini getir
                var defaultStartDate = DateTime.Now.AddDays(-30);
                var defaultEndDate = DateTime.Now;
                transactions = await _transactionRepository.GetTransactionsBetweenDatesAsync(
                    defaultStartDate, defaultEndDate);
            }

            // Sayfalama uygula
            var pagedTransactions = transactions
                .Skip(offset)
                .Take(limit)
                .ToList();

            _logger.LogInformation("Sayfalanmış transaction sayısı: {Count}", pagedTransactions.Count);

            // Performans optimizasyonu: Tüm analiz sonuçlarını ve risk değerlendirmelerini batch olarak al
            var transactionIds = pagedTransactions.Select(t => t.Id).ToList();
            
            // Tüm analiz sonuçlarını tek seferde al
            var analysisResultsTask = GetAnalysisResultsBatchAsync(transactionIds);
            
            // Tüm risk değerlendirmelerini tek seferde al
            var riskEvaluationsTask = GetRiskEvaluationsBatchAsync(transactionIds);
            
            // Her iki sorguyu paralel çalıştır
            await Task.WhenAll(analysisResultsTask, riskEvaluationsTask);
            
            var analysisResults = await analysisResultsTask;
            var riskEvaluations = await riskEvaluationsTask;

            _logger.LogInformation("Analiz sonuçları alındı: {Count}, Risk değerlendirmeleri alındı: {Count}", 
                analysisResults.Count, riskEvaluations.Count);

            // Response'ları oluştur
            var response = pagedTransactions.Select(t =>
            {
                // İlgili analiz sonucunu bul
                var analysisResult = analysisResults.FirstOrDefault(ar => ar.TransactionId == t.Id);
                
                // İlgili risk değerlendirmesini bul (en son tarihli olanı)
                var riskEvaluation = riskEvaluations
                    .Where(re => re.TransactionId == t.Id)
                    .OrderByDescending(re => re.EvaluatedAt)
                    .FirstOrDefault();
                
                return new
                {
                    Id = t.Id,
                    TransactionId = t.Id,
                    UserId = t.UserId,
                    Amount = t.Amount,
                    MerchantId = t.MerchantId,
                    Type = t.Type.ToString(),
                    Status = t.Status.ToString(),
                    RiskScore = t.RiskScore?.Score ?? 0,
                    RiskLevel = t.RiskScore?.Level.ToString() ?? "Unknown",
                    TriggeredRules = analysisResult?.TriggeredRuleCount ?? 0, // Analiz sonucundan al
                    Timestamp = t.TransactionTime,
                    IpAddress = t.DeviceInfo?.IpAddress ?? "Unknown",
                    DeviceId = t.DeviceInfo?.DeviceId ?? "Unknown",
                    Location = new
                    {
                        Country = t.Location?.Country ?? "Unknown",
                        City = t.Location?.City ?? "Unknown",
                        Latitude = t.Location?.Latitude,
                        Longitude = t.Location?.Longitude
                    },
                    // Context durumları - analiz sonucuna göre
                    Contexts = new
                    {
                        Transaction = analysisResult != null,
                        Account = analysisResult != null,
                        Ip = analysisResult != null,
                        Device = analysisResult != null,
                        Session = analysisResult != null,
                        MlModel = riskEvaluation != null,
                        Comprehensive = analysisResult != null && riskEvaluation != null
                    },
                    // Analiz sonuçları
                    Results = analysisResult != null ? new
                    {
                        FraudProbability = analysisResult.FraudProbability,
                        AnomalyScore = analysisResult.AnomalyScore,
                        Decision = analysisResult.Decision.ToString(),
                        AnalyzedAt = analysisResult.AnalyzedAt,
                        TriggeredRulesList = analysisResult.TriggeredRules?.Take(5).ToList() ?? new List<TriggeredRuleInfo>(),
                        // ML Analysis - RiskEvaluation'dan al
                        MLAnalysis = riskEvaluation != null ? ExtractMLAnalysisFromRiskEvaluation(riskEvaluation) : (object?)null
                    } : (object?)null
                };
            }).ToList();

            return Ok(new
            {
                Success = true,
                Total = transactions.Count,
                Count = pagedTransactions.Count,
                Offset = offset,
                Limit = limit,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlemler listelenirken hata oluştu");
            return StatusCode(500, new { Error = "İşlemler listelenirken hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// Birden fazla transaction için analiz sonuçlarını batch olarak alır
    /// </summary>
    private async Task<List<AnalysisResult>> GetAnalysisResultsBatchAsync(List<Guid> transactionIds)
    {
        if (!transactionIds.Any()) return new List<AnalysisResult>();
        
        try
        {
            // Tek sorguda tüm analiz sonuçlarını al
            return await _analysisResultRepository.GetByTransactionIdsAsync(transactionIds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analiz sonuçları batch olarak alınırken hata oluştu");
            return new List<AnalysisResult>();
        }
    }

    /// <summary>
    /// Birden fazla transaction için risk değerlendirmelerini batch olarak alır
    /// </summary>
    private async Task<List<RiskEvaluation>> GetRiskEvaluationsBatchAsync(List<Guid> transactionIds)
    {
        if (!transactionIds.Any()) return new List<RiskEvaluation>();
        
        try
        {
            // Doğrudan transaction_id ile sorgula (entity_type/entity_id boş olduğu için)
            var evaluations = await _riskEvaluationRepository.GetByTransactionIdsAsync(transactionIds);
            return evaluations.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Risk değerlendirmeleri batch olarak alınırken hata oluştu");
            return new List<RiskEvaluation>();
        }
    }

    /// <summary>
    /// Belirtilen kullanıcının işlemlerini getirir
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserTransactions(
        string userId,
        [FromQuery] int limit = 50)
    {
        try
        {
            _logger.LogInformation("Kullanıcı işlemleri getiriliyor: {UserId}", userId);

            var transactions = await _transactionRepository.GetUserTransactionHistoryAsync(userId, limit);

            var response = transactions.Select(t => new
            {
                Id = t.Id,
                TransactionId = t.Id,
                Amount = t.Amount,
                MerchantId = t.MerchantId,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                RiskScore = t.RiskScore?.Score ?? 0,
                RiskLevel = t.RiskScore?.Level.ToString() ?? "Unknown",
                Timestamp = t.TransactionTime,
                Location = new
                {
                    Country = t.Location?.Country ?? "Unknown",
                    City = t.Location?.City ?? "Unknown"
                }
            });

            return Ok(new
            {
                Success = true,
                UserId = userId,
                Count = transactions.Count,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı işlemleri getirilirken hata oluştu: {UserId}", userId);
            return StatusCode(500, new { Error = "Kullanıcı işlemleri getirilirken hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// Belirli bir işlemin detaylarını getirir
    /// </summary>
    [HttpGet("{transactionId}")]
    public async Task<IActionResult> GetTransaction(Guid transactionId)
    {
        try
        {
            _logger.LogInformation("İşlem detayları getiriliyor: {TransactionId}", transactionId);

            var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
            
            if (transaction == null)
            {
                return NotFound(new { Error = "İşlem bulunamadı", TransactionId = transactionId });
            }

            // Analiz sonuçlarını al
            var analysisResult = await _analysisResultRepository.GetByTransactionIdAsync(transaction.Id);
            
            // Risk değerlendirmesini al (ML model detayları için)
            var riskEvaluations = await _riskEvaluationRepository.GetByTransactionIdsAsync(new List<Guid> { transaction.Id });
            var riskEvaluation = riskEvaluations?.OrderByDescending(re => re.EvaluatedAt).FirstOrDefault();

            var response = new
            {
                Id = transaction.Id,
                TransactionId = transaction.Id,
                UserId = transaction.UserId,
                Amount = transaction.Amount,
                MerchantId = transaction.MerchantId,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Timestamp = transaction.TransactionTime,
                RiskScore = transaction.RiskScore?.Score ?? 0,
                RiskLevel = transaction.RiskScore?.Level.ToString() ?? "Unknown",
                TriggeredRules = analysisResult?.TriggeredRuleCount ?? 0,
                Details = transaction.Details,
                DeviceInfo = transaction.DeviceInfo,
                Location = transaction.Location,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.LastModifiedAt,
                
                // Detaylı analiz sonuçları
                AnalysisResults = analysisResult != null ? new
                {
                    FraudProbability = analysisResult.FraudProbability,
                    AnomalyScore = analysisResult.AnomalyScore,
                    Decision = analysisResult.Decision.ToString(),
                    Status = analysisResult.Status.ToString(),
                    AnalyzedAt = analysisResult.AnalyzedAt,
                    TotalRuleCount = analysisResult.TotalRuleCount,
                    TriggeredRuleCount = analysisResult.TriggeredRuleCount,
                    TriggeredRules = analysisResult.TriggeredRules ?? new List<TriggeredRuleInfo>(),
                    RiskFactors = analysisResult.RiskFactors == null ? new List<object>() : 
                        analysisResult.RiskFactors.Select(rf => (object)new
                        {
                            Type = rf.Type.ToString(),
                            Description = rf.Description,
                            Confidence = rf.Confidence,
                            Severity = rf.Severity.ToString(),
                            Source = rf.Source ?? "Unknown",
                            DetectedAt = rf.DetectedAt
                        }).ToList(),
                    // ML Analysis - RiskEvaluation'dan al
                    MLAnalysis = riskEvaluation != null ? ExtractMLAnalysisFromRiskEvaluation(riskEvaluation) : (object?)null,
                    AppliedActions = analysisResult.AppliedActions ?? new List<string>(),
                    Error = analysisResult.Error
                } : (object?)null,
                
                // Ayrıca RiskEvaluation detaylarını da ekle
                RiskEvaluationDetails = riskEvaluation != null ? new
                {
                    Id = riskEvaluation.Id,
                    EvaluatedAt = riskEvaluation.EvaluatedAt,
                    MLScore = riskEvaluation.MLScore,
                    RuleBasedScore = riskEvaluation.RuleBasedScore,
                    ConfidenceScore = riskEvaluation.ConfidenceScore,
                    EnsembleWeight = riskEvaluation.EnsembleWeight,
                    ProcessingTimeMs = riskEvaluation.ProcessingTimeMs,
                    ModelVersion = riskEvaluation.ModelVersion,
                    EvaluationSource = riskEvaluation.EvaluationSource,
                    UsedAlgorithms = riskEvaluation.UsedAlgorithms,
                    Explanation = riskEvaluation.Explanation,
                    RecommendedAction = riskEvaluation.RecommendedAction,
                    ModelMetrics = riskEvaluation.ModelMetrics,
                    FeatureValues = riskEvaluation.FeatureValues,
                    Errors = riskEvaluation.Errors,
                    Warnings = riskEvaluation.Warnings
                } : (object?)null
            };

            return Ok(new
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem detayları getirilirken hata oluştu: {TransactionId}", transactionId);
            return StatusCode(500, new { Error = "İşlem detayları getirilirken hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// Tarih aralığına göre işlemleri getirir
    /// </summary>
    [HttpGet("date-range")]
    public async Task<IActionResult> GetTransactionsByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Tarih aralığına göre işlemler getiriliyor: {StartDate} - {EndDate}", 
                startDate, endDate);

            if (startDate > endDate)
            {
                return BadRequest(new { Error = "Başlangıç tarihi bitiş tarihinden büyük olamaz" });
            }

            var transactions = await _transactionRepository.GetTransactionsBetweenDatesAsync(startDate, endDate);

            var response = transactions.Select(t => new
            {
                Id = t.Id,
                TransactionId = t.Id,
                UserId = t.UserId,
                Amount = t.Amount,
                MerchantId = t.MerchantId,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                RiskScore = t.RiskScore?.Score ?? 0,
                RiskLevel = t.RiskScore?.Level.ToString() ?? "Unknown",
                Timestamp = t.TransactionTime,
                IpAddress = t.DeviceInfo?.IpAddress ?? "Unknown",
                DeviceId = t.DeviceInfo?.DeviceId ?? "Unknown",
                Location = new
                {
                    Country = t.Location?.Country ?? "Unknown",
                    City = t.Location?.City ?? "Unknown"
                }
            });

            return Ok(new
            {
                Success = true,
                StartDate = startDate,
                EndDate = endDate,
                Count = transactions.Count,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tarih aralığına göre işlemler getirilirken hata oluştu");
            return StatusCode(500, new { Error = "İşlemler getirilirken hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// Risk seviyesine göre işlemleri filtreler
    /// </summary>
    [HttpGet("by-risk/{riskLevel}")]
    public async Task<IActionResult> GetTransactionsByRiskLevel(string riskLevel)
    {
        try
        {
            _logger.LogInformation("Risk seviyesine göre işlemler getiriliyor: {RiskLevel}", riskLevel);

            // Son 30 günün işlemlerini getir
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;
            var allTransactions = await _transactionRepository.GetTransactionsBetweenDatesAsync(startDate, endDate);

            // Risk seviyesine göre filtrele
            var filteredTransactions = allTransactions
                .Where(t => t.RiskScore != null && 
                           string.Equals(t.RiskScore.Level.ToString(), riskLevel, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var response = filteredTransactions.Select(t => new
            {
                Id = t.Id,
                TransactionId = t.Id,
                UserId = t.UserId,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                RiskScore = t.RiskScore.Score,
                RiskLevel = t.RiskScore.Level.ToString(),
                Timestamp = t.TransactionTime
            });

            return Ok(new
            {
                Success = true,
                RiskLevel = riskLevel,
                Count = filteredTransactions.Count,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk seviyesine göre işlemler getirilirken hata oluştu");
            return StatusCode(500, new { Error = "İşlemler getirilirken hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// İşlem istatistiklerini getirir
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetTransactionStatistics()
    {
        try
        {
            _logger.LogInformation("İşlem istatistikleri getiriliyor");

            // Son 30 günün işlemlerini getir
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;
            var transactions = await _transactionRepository.GetTransactionsBetweenDatesAsync(startDate, endDate);

            var stats = new
            {
                TotalTransactions = transactions.Count,
                TotalAmount = transactions.Sum(t => t.Amount),
                AverageAmount = transactions.Any() ? transactions.Average(t => t.Amount) : 0,
                RiskLevelDistribution = transactions
                    .Where(t => t.RiskScore != null)
                    .GroupBy(t => t.RiskScore.Level.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                TypeDistribution = transactions
                    .GroupBy(t => t.Type.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                DailyTransactionCount = transactions
                    .GroupBy(t => t.TransactionTime.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count())
            };

            return Ok(new
            {
                Success = true,
                Period = new { StartDate = startDate, EndDate = endDate },
                Statistics = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem istatistikleri getirilirken hata oluştu");
            return StatusCode(500, new { Error = "İstatistikler getirilirken hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// İşlemin tam detaylarını getirir (Transaction, AnalysisResult, RiskFactors)
    /// </summary>
    [HttpGet("complete-details/{transactionId}")]
    public async Task<IActionResult> GetTransactionCompleteDetails(Guid transactionId)
    {
        try
        {
            _logger.LogInformation("İşlem tam detayları getiriliyor: {TransactionId}", transactionId);

            // 1. Transaction bilgilerini al
            var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
            
            if (transaction == null)
            {
                return NotFound(new { Error = "İşlem bulunamadı", TransactionId = transactionId });
            }

            // 2. AnalysisResult bilgilerini al
            var analysisResult = await _analysisResultRepository.GetByTransactionIdAsync(transaction.Id);

            // 3. RiskFactors bilgilerini al
            var riskFactors = new List<object>();
            if (analysisResult != null)
            {
                var riskFactorEntities = await _riskFactorRepository.GetByAnalysisResultIdAsync(analysisResult.Id);
                riskFactors = riskFactorEntities.Select(rf => new
                {
                    Id = rf.Id,
                    Code = rf.Code,
                    Description = rf.Description,
                    Confidence = rf.Confidence,
                    Severity = rf.Severity.ToString(),
                    Source = rf.Source,
                    RuleId = rf.RuleId,
                    ActionTaken = rf.ActionTaken,
                    Type = rf.Type.ToString(),
                    DetectedAt = rf.DetectedAt
                }).ToList<object>();
            }

            // Response'u oluştur
            var response = new
            {
                Transaction = new
                {
                    Id = transaction.Id,
                    TransactionId = transaction.Id,
                    UserId = transaction.UserId,
                    Amount = transaction.Amount,
                    TransactionTime = transaction.TransactionTime,
                    Type = transaction.Type.ToString(),
                    Status = transaction.Status.ToString(),
                    MerchantId = transaction.MerchantId,
                    DeviceInfo = transaction.DeviceInfo,
                    Location = transaction.Location
                },
                AnalysisResult = analysisResult != null ? new
                {
                    Id = analysisResult.Id,
                    TransactionId = analysisResult.TransactionId,
                    FraudProbability = analysisResult.FraudProbability,
                    RiskScore = analysisResult.RiskScore,
                    RiskLevel = analysisResult.RiskScore?.Level.ToString(),
                    AnomalyScore = analysisResult.AnomalyScore,
                    Decision = analysisResult.Decision.ToString(),
                    AnalyzedAt = analysisResult.AnalyzedAt,
                    Status = analysisResult.Status.ToString()
                } : null,
                RiskFactors = riskFactors
            };

            return Ok(new
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem tam detayları getirilirken hata oluştu: {TransactionId}", transactionId);
            return StatusCode(500, new { Error = "İşlem detayları getirilirken hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// RiskEvaluation'dan ML Analysis bilgilerini çıkarır
    /// </summary>
    private object ExtractMLAnalysisFromRiskEvaluation(RiskEvaluation riskEvaluation)
    {
        try
        {
            _logger.LogInformation("RiskEvaluation ML bilgileri - ModelInfo count: {Count}, FeatureImportance count: {FeatureCount}, UsedAlgorithms count: {AlgorithmCount}", 
                riskEvaluation.ModelInfo?.Count ?? 0, 
                riskEvaluation.FeatureImportance?.Count ?? 0,
                riskEvaluation.UsedAlgorithms?.Count ?? 0);
                
            // Basit ML analysis bilgileri
            var primaryModel = "Unknown";
            var ensembleAvailable = false;
            var lightGbmAvailable = false;
            var pcaAvailable = false;
            
            // ModelInfo'dan temel bilgileri çıkar
            if (riskEvaluation.ModelInfo != null && riskEvaluation.ModelInfo.Any())
            {
                // PrimaryModel veya ModelUsed alanlarını kontrol et
                if (riskEvaluation.ModelInfo.TryGetValue("PrimaryModel", out var primaryModelObj))
                {
                    primaryModel = primaryModelObj?.ToString() ?? "Unknown";
                }
                else if (riskEvaluation.ModelInfo.TryGetValue("ModelUsed", out var modelUsedObj))
                {
                    primaryModel = modelUsedObj?.ToString() ?? "Unknown";
                }
                
                // Model availability'yi kontrol et
                if (riskEvaluation.ModelInfo.ContainsKey("EnsembleAvailable"))
                {
                    ensembleAvailable = true;
                }
                if (riskEvaluation.ModelInfo.ContainsKey("LightGBM"))
                {
                    lightGbmAvailable = true;
                }
                if (riskEvaluation.ModelInfo.ContainsKey("PCA"))
                {
                    pcaAvailable = true;
                }
            }
            
            // Kullanılan algoritmalar
            var usedAlgorithms = riskEvaluation.UsedAlgorithms ?? new List<string>();
            
            // Feature importance
            var featureImportance = riskEvaluation.FeatureImportance ?? new Dictionary<string, double>();
            
            // Ensemble kullanım durumu
            var ensembleUsed = primaryModel.Contains("Ensemble", StringComparison.OrdinalIgnoreCase) || 
                              usedAlgorithms.Any(a => a.Contains("Ensemble", StringComparison.OrdinalIgnoreCase));
            
            return new
            {
                PrimaryModel = primaryModel,
                Confidence = riskEvaluation.ConfidenceScore,
                EnsembleUsed = ensembleUsed,
                ModelHealth = new
                {
                    EnsembleAvailable = ensembleAvailable,
                    LightGBMAvailable = lightGbmAvailable,
                    PCAAvailable = pcaAvailable,
                    FallbackUsed = riskEvaluation.Errors?.Any(e => e.Contains("fallback")) ?? false,
                    ErrorCount = riskEvaluation.Errors?.Count ?? 0,
                    WarningCount = riskEvaluation.Warnings?.Count ?? 0
                },
                ProcessingTime = riskEvaluation.ProcessingTimeMs ?? 0,
                FeatureImportance = featureImportance,
                UsedAlgorithms = usedAlgorithms,
                MLScore = riskEvaluation.MLScore,
                RuleBasedScore = riskEvaluation.RuleBasedScore,
                EnsembleWeight = riskEvaluation.EnsembleWeight
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RiskEvaluation'dan ML analiz bilgileri çıkarılırken hata oluştu");
            return new
            {
                PrimaryModel = "Unknown",
                Confidence = riskEvaluation.ConfidenceScore,
                EnsembleUsed = false,
                ProcessingTime = riskEvaluation.ProcessingTimeMs ?? 0,
                Error = "ML analiz bilgileri çıkarılamadı"
            };
        }
    }
} 