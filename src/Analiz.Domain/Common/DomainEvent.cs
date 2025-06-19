using MediatR;

namespace FraudShield.TransactionAnalysis.Domain.Common;

public abstract class DomainEvent : INotification
{
    public DateTime Timestamp { get; protected set; }

    protected DomainEvent()
    {
        Timestamp = DateTime.UtcNow;
    }
}