using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class TransactionCreatedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public string UserId { get; }
    public decimal Amount { get; }
    public DateTime TransactionTime { get; }

    public TransactionCreatedEvent(
        Guid transactionId,
        string userId,
        decimal amount,
        DateTime transactionTime)
    {
        TransactionId = transactionId;
        UserId = userId;
        Amount = amount;
        TransactionTime = transactionTime;
    }
}