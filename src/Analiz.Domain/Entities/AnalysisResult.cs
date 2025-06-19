using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Analiz.Domain.Models;
using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Analiz.Domain.Entities;
/// <summary>
/// Geliştirilmiş AnalysisResult - Net model bilgileri ile
/// </summary>
public class AnalysisResult : Entity
{
    public Guid TransactionId { get; private set; }
    public double AnomalyScore { get; private set; }
    public double FraudProbability { get; private set; }
    public RiskScore RiskScore { get; private set; }
    public virtual ICollection<RiskFactor> RiskFactors { get; private set; } = new List<RiskFactor>();
    public DecisionType Decision { get; private set; }
    public DateTime AnalyzedAt { get; private set; }
    public AnalysisStatus Status { get; private set; }
    public string? Error { get; private set; }

    // Rule-based analysis
    public int TotalRuleCount { get; private set; }
    public int TriggeredRuleCount { get; private set; }
    public List<string> AppliedActions { get; private set; } = new List<string>();
    public List<TriggeredRuleInfo> TriggeredRules { get; private set; } = new List<TriggeredRuleInfo>();
    
    // ML-based analysis - Geliştirilmiş
    [NotMapped]
    public MLAnalysisResult? MLAnalysis { get; private set; }
    
    // Combined results
    public virtual FraudAlert FraudAlert { get; private set; }

    // Navigation Properties
    public virtual Transaction Transaction { get; private set; }

    private AnalysisResult()
    {
        RiskFactors = new List<RiskFactor>();
    }

    public static AnalysisResult Create(
        Guid transactionId,
        double anomalyScore,
        double fraudProbability,
        RiskLevel riskLevel,
        DecisionType decision)
    {
        var riskScore = new RiskScore(fraudProbability, new List<string>());

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            AnomalyScore = anomalyScore,
            FraudProbability = fraudProbability,
            RiskScore = riskScore,
            Decision = decision,
            AnalyzedAt = DateTime.Now,
            Status = AnalysisStatus.Completed
        };
    }

    public void AddTriggeredRule(TriggeredRuleInfo rule)
    {
        TriggeredRules.Add(rule);
        TriggeredRuleCount = TriggeredRules.Count;
    }

    public void AddAppliedAction(string action)
    {
        if (!AppliedActions.Contains(action))
            AppliedActions.Add(action);
    }

    public static AnalysisResult CreateFailed(Guid transactionId, string errorMessage)
    {
        var result = new AnalysisResult
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            Status = AnalysisStatus.Failed,
            Error = errorMessage,
            AnalyzedAt = DateTime.Now,
            Decision = DecisionType.ReviewRequired
        };

        return result;
    }

    public void AddRiskFactor(RiskFactor factor)
    {
        factor.AnalysisResultId = this.Id;
        RiskFactors.Add(factor);
    }

    public void SetError(string error)
    {
        Error = error;
        Status = AnalysisStatus.Failed;
    }

    public FraudAlert CreateFraudAlert(Guid userId, List<string> factors)
    {
        var alert = FraudAlert.Create(
            this.TransactionId,
            userId,
            this.RiskScore,
            factors);
            
        alert.SetAnalysisResult(this);
        this.FraudAlert = alert;
        
        return alert;
    }

    /// <summary>
    /// Analysis sonuç özeti
    /// </summary>
    public AnalysisSummary GetSummary()
    {
        return new AnalysisSummary
        {
            TransactionId = TransactionId,
            OverallRisk = RiskScore.Level.ToString(),
            FraudProbability = FraudProbability,
            AnomalyScore = AnomalyScore,
            Decision = Decision.ToString(),
            
            // Rule-based summary
            RuleBasedAnalysis = new RuleBasedSummary
            {
                TotalRules = TotalRuleCount,
                TriggeredRules = TriggeredRuleCount,
                AppliedActions = AppliedActions
            },
            
            // ML-based summary
            MLBasedAnalysis = MLAnalysis != null ? new MLBasedSummary
            {
                PrimaryModel = MLAnalysis.PrimaryModel.ModelType,
                Confidence = MLAnalysis.Confidence,
                EnsembleUsed = MLAnalysis.PrimaryModel.IsEnsemble,
                ModelHealth = $"Ensemble: {MLAnalysis.ModelHealth.EnsembleAvailable}, " +
                             $"LightGBM: {MLAnalysis.ModelHealth.LightGBMAvailable}, " +
                             $"PCA: {MLAnalysis.ModelHealth.PCAAvailable}",
                ProcessingTime = MLAnalysis.ProcessingTimeMs
            } : null,
            
            // Risk factors summary
            RiskFactorsSummary = new RiskFactorsSummary
            {
                TotalCount = RiskFactors.Count,
                HighSeverityCount = RiskFactors.Count(rf => rf.Severity == RiskLevel.High || rf.Severity == RiskLevel.Critical),
                MLBasedCount = RiskFactors.Count(rf => rf.Source?.Contains("ML") == true || rf.Source?.Contains("Ensemble") == true),
                RuleBasedCount = RiskFactors.Count(rf => rf.Source == "Rule" || rf.Source?.Contains("Rule") == true)
            },
            
            IsSuccessful = Status == AnalysisStatus.Completed,
            AnalysisTime = AnalyzedAt
        };
    }
}

/// <summary>
/// ML analiz sonucu
/// </summary>
public class MLAnalysisResult
{
    public PrimaryModelInfo PrimaryModel { get; set; }
    public Dictionary<string, ModelScoreInfo> ModelScores { get; set; } = new();
    public ModelHealthInfo ModelHealth { get; set; }
    public double Confidence { get; set; }
    public long ProcessingTimeMs { get; set; }
    public Dictionary<string, double> FeatureImportance { get; set; } = new();
    public List<string> UsedAlgorithms { get; set; } = new();
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// Ana model bilgisi
/// </summary>
public class PrimaryModelInfo
{
    public string ModelType { get; set; }
    public string ModelSource { get; set; }
    public double FraudProbability { get; set; }
    public double AnomalyScore { get; set; }
    public bool IsEnsemble { get; set; }
    public bool IsSuccessful { get; set; }
}

/// <summary>
/// Model skor bilgisi
/// </summary>
public class ModelScoreInfo
{
    public double Probability { get; set; }
    public double Score { get; set; }
    public double AnomalyScore { get; set; }
    public bool IsAvailable { get; set; }
    public string Source { get; set; } // Primary, SubModel, Fallback
}

/// <summary>
/// Model sağlık bilgisi
/// </summary>
public class ModelHealthInfo
{
    public bool EnsembleAvailable { get; set; }
    public bool LightGBMAvailable { get; set; }
    public bool PCAAvailable { get; set; }
    public bool FallbackUsed { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
}

/// <summary>
/// Analiz özeti
/// </summary>
public class AnalysisSummary
{
    public Guid TransactionId { get; set; }
    public string OverallRisk { get; set; }
    public double FraudProbability { get; set; }
    public double AnomalyScore { get; set; }
    public string Decision { get; set; }
    public RuleBasedSummary RuleBasedAnalysis { get; set; }
    public MLBasedSummary? MLBasedAnalysis { get; set; }
    public RiskFactorsSummary RiskFactorsSummary { get; set; }
    public bool IsSuccessful { get; set; }
    public DateTime AnalysisTime { get; set; }
}

public class RuleBasedSummary
{
    public int TotalRules { get; set; }
    public int TriggeredRules { get; set; }
    public List<string> AppliedActions { get; set; } = new();
}

public class MLBasedSummary
{
    public string PrimaryModel { get; set; }
    public double Confidence { get; set; }
    public bool EnsembleUsed { get; set; }
    public string ModelHealth { get; set; }
    public long ProcessingTime { get; set; }
}

public class RiskFactorsSummary
{
    public int TotalCount { get; set; }
    public int HighSeverityCount { get; set; }
    public int MLBasedCount { get; set; }
    public int RuleBasedCount { get; set; }
}