namespace FraudShield.TransactionAnalysis.Domain.Enums;

public enum RiskFactorType
{
    ModelFeature,
    AnomalyDetection,
    HighValue,
    Location,
    Frequency,
    TimePattern,
    RuleViolation,
    UserBehavior,
    DeviceAnomaly,
    Time,
    Device,
    IPAddress,
    Velocity,
    RecurringPattern
}