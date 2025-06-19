using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class HighRiskTransactionDetectedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public string UserId { get; }
    public RiskScore RiskScore { get; }
    public List<string> RiskFactors { get; }
    public DateTime DetectedAt { get; }

    public HighRiskTransactionDetectedEvent(
        Guid transactionId,
        string userId,
        RiskScore riskScore,
        List<string> riskFactors)
    {
        TransactionId = transactionId;
        UserId = userId;
        RiskScore = riskScore;
        RiskFactors = riskFactors;
        DetectedAt = DateTime.UtcNow;
    }
}