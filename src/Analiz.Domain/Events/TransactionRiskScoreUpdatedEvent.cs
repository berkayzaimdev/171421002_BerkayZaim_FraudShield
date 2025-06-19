using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class TransactionRiskScoreUpdatedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public RiskScore Score { get; }

    public TransactionRiskScoreUpdatedEvent(Guid transactionId, RiskScore score)
    {
        TransactionId = transactionId;
        Score = score;
    }
}