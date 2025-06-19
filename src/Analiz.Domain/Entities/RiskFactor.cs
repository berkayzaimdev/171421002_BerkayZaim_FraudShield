using System.Text.Json.Serialization;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class RiskFactor
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string Code { get; set; }
    public RiskFactorType Type { get; set; }
    public string Description { get; set; }
    public double Confidence { get; set; }
    public RiskLevel Severity { get; set; }
    public Guid AnalysisResultId { get; set; }

    [JsonIgnore] public virtual AnalysisResult AnalysisResult { get; set; }

    public string? RuleId { get; set; } // İlişkili kural ID'si
    public string? ActionTaken { get; set; } // Alınan aksiyon
    public string Source { get; set; } = "Rule"; // Kaynağı (Rule, ML, VFactor)
    public DateTime DetectedAt { get; set; } = DateTime.Now;

    public static RiskFactor Create(RiskFactorType type, string description, double confidence)
    {
        return new RiskFactor
        {
            Type = type,
            Code = type.ToString(),
            Description = description,
            Confidence = confidence,
            Severity = ConvertConfidenceToSeverity(confidence)
        };
    }

    private static RiskLevel ConvertConfidenceToSeverity(double confidence)
    {
        return confidence switch
        {
            >= 0.85 => RiskLevel.Critical,
            >= 0.6 => RiskLevel.High,
            >= 0.4 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }

    public void SetTransactionId(Guid transactionId)
    {
        TransactionId = transactionId;
    }
}