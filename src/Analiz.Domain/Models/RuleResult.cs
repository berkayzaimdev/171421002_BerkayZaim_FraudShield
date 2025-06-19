using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Domain.Entities;

public class RuleResult
{
    public string RuleId { get; set; }
    public string RuleName { get; set; }
    public string RuleDescription { get; set; }
    public bool IsTriggered { get; set; }
    public double Confidence { get; set; }
    public double Score { get; set; }
    public RuleAction Action { get; set; }
}