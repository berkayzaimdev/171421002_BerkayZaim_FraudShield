using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Events;

public class TransactionStatusUpdatedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public TransactionStatus NewStatus { get; }
    public DateTime UpdatedAt { get; }

    public TransactionStatusUpdatedEvent(Guid transactionId, TransactionStatus newStatus)
    {
        TransactionId = transactionId;
        NewStatus = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}