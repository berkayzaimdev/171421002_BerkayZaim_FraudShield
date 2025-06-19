using Analiz.Domain.ValueObjects;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// İşlem analiz yanıtı - Veritabanı yapısına uygun
/// </summary>
public class TransactionAnalysisResponse
{
    // Transaction bilgileri
    public Guid TransactionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime TransactionTime { get; set; }
    
    // Risk bilgileri (Transaction'dan)
    public double? RiskScore { get; set; }
    public string? RiskLevel { get; set; }
    
    // AnalysisResult bilgileri
    public double FraudProbability { get; set; }
    public double AnomalyScore { get; set; }
    public string Decision { get; set; } = string.Empty;
    public int TriggeredRuleCount { get; set; }
    public List<string> AppliedActions { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
    public string AnalysisStatus { get; set; } = string.Empty;
    
    // Risk faktörleri
    public List<RiskFactorInfo> RiskFactors { get; set; } = new();
    
    // Genel bilgiler
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Context analiz yanıtı
/// </summary>
public class ContextAnalysisResponse
{
    public string ContextType { get; set; } = string.Empty;
    public Guid TransactionId { get; set; }
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public int TriggeredRules { get; set; }
    public List<string> AppliedActions { get; set; } = new();
    public double FraudProbability { get; set; }
    public double AnomalyScore { get; set; }
    public string Decision { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// ML model değerlendirme yanıtı
/// </summary>
public class MLModelEvaluationResponse
{
    public Guid TransactionId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public double FraudProbability { get; set; }
    public double AnomalyScore { get; set; }
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool EnsembleUsed { get; set; }
    public int ProcessingTime { get; set; }
    public Dictionary<string, double> FeatureImportance { get; set; } = new();
    public Dictionary<string, double> ModelMetrics { get; set; } = new();
    public DateTime EvaluatedAt { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Kapsamlı analiz yanıtı
/// </summary>
public class ComprehensiveAnalysisResponse
{
    public Guid TransactionId { get; set; }
    public double OverallRiskScore { get; set; }
    public string OverallRiskLevel { get; set; } = string.Empty;
    public double FraudProbability { get; set; }
    public double AnomalyScore { get; set; }
    public string Decision { get; set; } = string.Empty;
    public int TotalTriggeredRules { get; set; }
    public List<string> AppliedActions { get; set; } = new();
    public Dictionary<string, object> ContextResults { get; set; } = new();
    public object? MLAnalysis { get; set; }
    public int ProcessingTime { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// İşlem detay yanıtı - Veritabanı yapısına uygun
/// </summary>
public class TransactionDetailsResponse
{
    public TransactionInfo Transaction { get; set; } = new();
    public AnalysisResultInfo? AnalysisResult { get; set; }
    public RiskEvaluationInfo? RiskEvaluation { get; set; }
}

/// <summary>
/// İşlem bilgileri
/// </summary>
public class TransactionInfo
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime TransactionTime { get; set; }
    public double? RiskScore { get; set; }
    public string? RiskLevel { get; set; }
    public DeviceInfo? DeviceInfo { get; set; }
    public Location? Location { get; set; }
}

/// <summary>
/// Analiz sonucu bilgileri - AnalysisResult tablosundan
/// </summary>
public class AnalysisResultInfo
{
    public Guid Id { get; set; }
    public double FraudProbability { get; set; }
    public double AnomalyScore { get; set; }
    public string Decision { get; set; } = string.Empty;
    public int TriggeredRuleCount { get; set; }
    public List<string> AppliedActions { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<RiskFactorInfo> RiskFactors { get; set; } = new();
}

/// <summary>
/// Risk faktörü bilgileri - RiskFactor tablosundan
/// </summary>
public class RiskFactorInfo
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Risk değerlendirme bilgileri - RiskEvaluation tablosundan
/// </summary>
public class RiskEvaluationInfo
{
    public Guid Id { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public double MLScore { get; set; }
    public double RuleBasedScore { get; set; }
    public double ConfidenceScore { get; set; }
    public int ProcessingTime { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public List<string> UsedAlgorithms { get; set; } = new();
    public Dictionary<string, double> FeatureImportance { get; set; } = new();
} 