using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class TransactionDecision
{
    public string DecisionId { get; set; }
    public DecisionType Type { get; set; }
    public string Reason { get; set; }
    public DateTime DecisionTime { get; set; }
    public Dictionary<string, string> AdditionalInfo { get; set; }
}