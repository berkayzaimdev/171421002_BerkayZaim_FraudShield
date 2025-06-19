using System.Text.Json.Serialization;
using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class FraudAlert : Entity
{
    public Guid TransactionId { get; private set; }
    public Guid UserId { get; private set; }
    public AlertType Type { get; private set; }
    public AlertStatus Status { get; private set; }
    public RiskScore RiskScore { get; private set; }
    public List<string> Factors { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string Resolution { get; private set; }
    public string? CreatedBy { get; private set; }
    public Guid AnalysisResultId { get; private set; }
    
    [JsonIgnore]
    public virtual AnalysisResult AnalysisResult { get; private set; }
    
    private FraudAlert()
    {
        Factors = new List<string>();
    }

    public static FraudAlert Create(
        Guid transactionId,
        Guid userId,
        RiskScore riskScore,
        List<string> factors)
    {
        return new FraudAlert
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            UserId = userId,
            Type = DetermineAlertType(riskScore.Level),
            Status = AlertStatus.Active,
            RiskScore = riskScore,
            Factors = factors,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
    }
    
    public void SetAnalysisResult(AnalysisResult analysisResult)
    {
        if (analysisResult == null)
            throw new ArgumentNullException(nameof(analysisResult));
            
        AnalysisResultId = analysisResult.Id;
        AnalysisResult = analysisResult;
    }
    
    public void Resolve(string resolution, string resolvedBy = "system")
    {
        Status = AlertStatus.Resolved;
        Resolution = resolution;
        ResolvedAt = DateTime.UtcNow;
        CreatedBy = resolvedBy;
    }

    public void Assign(string assignedTo)
    {
        CreatedBy = assignedTo;
        Status = AlertStatus.Investigating;
    }

    // Property setters for service layer
    public void UpdateStatus(AlertStatus status) => Status = status;
    public void SetResolvedAt(DateTime? resolvedAt) => ResolvedAt = resolvedAt;
    public void SetResolution(string resolution) => Resolution = resolution;
    public void SetCreatedBy(string createdBy) => CreatedBy = createdBy;

    private static AlertType DetermineAlertType(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Critical => AlertType.Critical,
            RiskLevel.High => AlertType.High,
            _ => AlertType.Medium
        };
    }
}