using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class AnalysisCompletedEvent : DomainEvent
{
    public Guid AnalysisId { get; }
    public Guid TransactionId { get; }
    public RiskScore RiskScore { get; }
    public List<RiskFactor> RiskFactors { get; }
    public DateTime CompletedAt { get; }

    public AnalysisCompletedEvent(
        Guid analysisId,
        Guid transactionId,
        RiskScore riskScore,
        List<RiskFactor> riskFactors)
    {
        AnalysisId = analysisId;
        TransactionId = transactionId;
        RiskScore = riskScore;
        RiskFactors = riskFactors;
        CompletedAt = DateTime.UtcNow;
    }
}