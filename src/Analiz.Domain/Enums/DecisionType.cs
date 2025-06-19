namespace FraudShield.TransactionAnalysis.Domain.Enums;

public enum DecisionType
{
    Approve,
    Deny,
    ReviewRequired,
    EscalateToManager,
    RequireAdditionalVerification
}