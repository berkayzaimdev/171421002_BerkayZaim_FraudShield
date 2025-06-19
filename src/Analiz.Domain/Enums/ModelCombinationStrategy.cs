namespace FraudShield.TransactionAnalysis.Domain.Enums;

public enum ModelCombinationStrategy
{
    WeightedAverage,
    Voting,
    MaxConfidence,
    MinimumRisk
}