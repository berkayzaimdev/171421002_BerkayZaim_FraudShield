using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class FraudAlertResolvedEvent : DomainEvent
{
    public Guid AlertId { get; }
    public string Resolution { get; }
    public DateTime ResolvedAt { get; }

    public FraudAlertResolvedEvent(Guid alertId, string resolution)
    {
        AlertId = alertId;
        Resolution = resolution;
        ResolvedAt = DateTime.UtcNow;
    }
}