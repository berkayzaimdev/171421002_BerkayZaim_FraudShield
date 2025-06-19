using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Events;

public class FraudAlertCreatedEvent : DomainEvent
{
    public Guid AlertId { get; }
    public Guid TransactionId { get; }
    public string UserId { get; }
    public AlertType Type { get; }
    public DateTime CreatedAt { get; }

    public FraudAlertCreatedEvent(
        Guid alertId,
        Guid transactionId,
        string userId,
        AlertType type)
    {
        AlertId = alertId;
        TransactionId = transactionId;
        UserId = userId;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }
}