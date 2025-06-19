namespace FraudShield.TransactionAnalysis.Domain.Enums;

public enum TransactionStatus
{
    Pending,
    Approved,
    RequiresReview,
    Blocked,
    Failed
}