using System;
using System.Collections.Generic;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Entities;

/// <summary>
/// Risk değerlendirme sonucu
/// </summary>
public class RiskEvaluation : Entity, ISoftDelete
{
    /// <summary>
    /// İşlem ID
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Değerlendirme zamanı
    /// </summary>
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fraud olma olasılığı (0-1 arası)
    /// </summary>
    public double FraudProbability { get; set; }

    /// <summary>
    /// Anomali skoru
    /// </summary>
    public double AnomalyScore { get; set; }

    /// <summary>
    /// Risk seviyesi
    /// </summary>
    public RiskLevel? RiskScore { get; set; }

    /// <summary>
    /// Risk faktörleri
    /// </summary>
    public List<RiskFactor> RiskFactors { get; set; } = new();

    /// <summary>
    /// Feature değerleri
    /// </summary>
    public Dictionary<string, double> FeatureValues { get; set; } = new();

    /// <summary>
    /// Feature önem dereceleri
    /// </summary>
    public Dictionary<string, double> FeatureImportance { get; set; } = new();

    /// <summary>
    /// Model metrikleri
    /// </summary>
    public Dictionary<string, double> ModelMetrics { get; set; } = new();

    /// <summary>
    /// Kullanılan model bilgileri
    /// </summary>
    public Dictionary<string, object> ModelInfo { get; set; } = new();

    /// <summary>
    /// Ek veriler
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// Güven skoru (0-1 arası)
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// ML tabanlı skor
    /// </summary>
    public double MLScore { get; set; }

    /// <summary>
    /// Kural tabanlı skor
    /// </summary>
    public double RuleBasedScore { get; set; }

    /// <summary>
    /// Ensemble ağırlığı
    /// </summary>
    public double EnsembleWeight { get; set; } = 1.0;

    /// <summary>
    /// Kullanılan algoritmalar
    /// </summary>
    public List<string> UsedAlgorithms { get; set; } = new();

    /// <summary>
    /// İşlem süresi (ms)
    /// </summary>
    public long? ProcessingTimeMs { get; set; }

    /// <summary>
    /// Hata mesajları
    /// </summary>
    public List<string>? Errors { get; set; } = new();

    /// <summary>
    /// Uyarı mesajları
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccessful => Errors.Count == 0;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Önerilen aksiyon
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// EntityType
    /// </summary>
    public string? EntityType { get; private set; }

    /// <summary>
    /// EntityId
    /// </summary>
    public Guid? EntityId { get; private set; }

    /// <summary>
    /// EvaluationType
    /// </summary>
    public string? EvaluationType { get; private set; }

    /// <summary>
    /// RiskData
    /// </summary>
    public Dictionary<string, object> RiskData { get; set; } = new();

    /// <summary>
    /// EvaluationTimestamp
    /// </summary>
    public DateTime? EvaluationTimestamp { get; set; }

    /// <summary>
    /// EvaluationSource
    /// </summary>
    public string? EvaluationSource { get; set; }

    /// <summary>
    /// ModelVersion
    /// </summary>
    public string? ModelVersion { get; set; }

    /// <summary>
    /// IsActive
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// CreatedAt
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// CreatedBy
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// LastModifiedAt
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// LastModifiedBy
    /// </summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// IsDeleted
    /// </summary>
    public bool? IsDeleted { get; set; }

    /// <summary>
    /// DeletedAt
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// DeletedBy
    /// </summary>
    public string? DeletedBy { get; set; }

    public RiskEvaluation() { } // For EF Core

    public RiskEvaluation(
        string entityType,
        Guid entityId,
        string evaluationType,
        Dictionary<string, object> riskData,
        string evaluationSource,
        string modelVersion)
    {
        EntityType = entityType;
        EntityId = entityId;
        EvaluationType = evaluationType;
        RiskData = riskData;
        EvaluationTimestamp = DateTime.UtcNow;
        EvaluationSource = evaluationSource;
        ModelVersion = modelVersion;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = "system";
    }



    public void UpdateRiskData(Dictionary<string, object> newRiskData, string modifiedBy)
    {
        RiskData = newRiskData;
        EvaluationTimestamp = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Deactivate(string deletedBy)
    {
        IsActive = false;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Reactivate(string modifiedBy)
    {
        IsActive = true;
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Risk değerlendirmesi oluştur
    /// </summary>
    public static RiskEvaluation Create(
        Guid transactionId,
        double fraudProbability,
        double anomalyScore,
        RiskLevel riskLevel)
    {
        return new RiskEvaluation
        {
            TransactionId = transactionId,
            FraudProbability = fraudProbability,
            AnomalyScore = anomalyScore,
            RiskScore = riskLevel,
            EvaluatedAt = DateTime.UtcNow,
            ConfidenceScore = CalculateConfidence(fraudProbability, anomalyScore)
        };
    }

    /// <summary>
    /// ML prediction'dan oluştur
    /// </summary>
    public static RiskEvaluation FromMLPrediction(
        Guid transactionId,
        ModelPrediction lightGbmPrediction,
        ModelPrediction pcaPrediction,
        RiskLevel riskLevel)
    {
        var evaluation = new RiskEvaluation
        {
            TransactionId = transactionId,
            FraudProbability = lightGbmPrediction.Probability,
            AnomalyScore = pcaPrediction.AnomalyScore,
            RiskScore = riskLevel,
            EvaluatedAt = DateTime.UtcNow,
            MLScore = (lightGbmPrediction.Probability + pcaPrediction.Probability) / 2.0,
            ConfidenceScore = CalculateConfidence(lightGbmPrediction.Probability, pcaPrediction.AnomalyScore)
        };

        // Model bilgilerini ekle
        evaluation.ModelInfo["LightGBM"] = new
        {
            Probability = lightGbmPrediction.Probability,
            Score = lightGbmPrediction.Score,
            ModelType = lightGbmPrediction.ModelType
        };

        evaluation.ModelInfo["PCA"] = new
        {
            AnomalyScore = pcaPrediction.AnomalyScore,
            Probability = pcaPrediction.Probability,
            ModelType = pcaPrediction.ModelType
        };

        evaluation.UsedAlgorithms.AddRange(new[] { "LightGBM", "PCA" });

        return evaluation;
    }

    /// <summary>
    /// Güven skoru hesapla
    /// </summary>
    private static double CalculateConfidence(double fraudProbability, double anomalyScore)
    {
        // Çok düşük veya çok yüksek değerler daha güvenilir
        var probabilityConfidence = fraudProbability < 0.2 || fraudProbability > 0.8 ? 0.9 : 0.7;
        var anomalyConfidence = anomalyScore < 0.5 || anomalyScore > 2.0 ? 0.8 : 0.6;

        return (probabilityConfidence + anomalyConfidence) / 2.0;
    }

    /// <summary>
    /// Risk faktörü ekle
    /// </summary>
    public void AddRiskFactor(string code, string description, double confidence, string severity = "Medium")
    {
        RiskFactors.Add(new RiskFactor
        {
            Code = code,
            Description = description,
            Confidence = confidence,
            Severity = Enum.Parse<RiskLevel>(severity, true),
            DetectedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Feature değeri ekle
    /// </summary>
    public void AddFeature(string featureName, double value, double importance = 0)
    {
        FeatureValues[featureName] = value;
        if (importance > 0)
        {
            FeatureImportance[featureName] = importance;
        }
    }

    /// <summary>
    /// Hata ekle
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add($"[{DateTime.UtcNow:HH:mm:ss}] {error}");
    }

    /// <summary>
    /// Uyarı ekle
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add($"[{DateTime.UtcNow:HH:mm:ss}] {warning}");
    }

    /// <summary>
    /// Risk seviyesi string'e
    /// </summary>
    public string RiskLevelText => RiskScore.ToString();

    /// <summary>
    /// Risk yüzdesi
    /// </summary>
    public double RiskPercentage => Math.Round(FraudProbability * 100, 2);

    /// <summary>
    /// Özet bilgi
    /// </summary>
    public override string ToString()
    {
        return $"RiskEvaluation: TransactionId={TransactionId}, " +
               $"RiskLevel={RiskScore}, FraudProbability={FraudProbability:F4}, " +
               $"AnomalyScore={AnomalyScore:F4}, Confidence={ConfidenceScore:F4}";
    }
}