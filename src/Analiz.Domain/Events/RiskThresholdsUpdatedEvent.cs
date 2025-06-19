using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class RiskThresholdsUpdatedEvent : DomainEvent
{
    public RiskThresholds Thresholds { get; }
    public DateTime UpdatedAt { get; }

    public RiskThresholdsUpdatedEvent(RiskThresholds thresholds)
    {
        Thresholds = thresholds;
        UpdatedAt = DateTime.UtcNow;
    }
}