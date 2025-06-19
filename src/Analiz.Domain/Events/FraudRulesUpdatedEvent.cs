using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Events;

public class FraudRulesUpdatedEvent : DomainEvent
{
    public List<FraudRule> UpdatedRules { get; }
    public DateTime UpdatedAt { get; }

    public FraudRulesUpdatedEvent(List<FraudRule> rules)
    {
        UpdatedRules = rules;
        UpdatedAt = DateTime.UtcNow;
    }
}