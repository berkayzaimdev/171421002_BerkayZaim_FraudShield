using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class TransactionFlagAddedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public TransactionFlag Flag { get; }
    public DateTime AddedAt { get; }

    public TransactionFlagAddedEvent(Guid transactionId, TransactionFlag flag)
    {
        TransactionId = transactionId;
        Flag = flag;
        AddedAt = DateTime.UtcNow;
    }
}