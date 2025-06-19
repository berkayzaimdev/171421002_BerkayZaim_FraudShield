using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class RiskScoringConfiguration
{
    public TimeSpan ProfileLookbackPeriod { get; set; } = TimeSpan.FromDays(30);
    public Dictionary<RiskLevel, double> RiskThresholds { get; set; }
    public double HighRiskThreshold { get; set; } = 0.75;
    public double UnusualAmountMultiplier { get; set; } = 3.0;
    public int VelocityCheckPeriodMinutes { get; set; } = 60;
    public int MaxTransactionsPerPeriod { get; set; } = 10;

    public void UpdateThresholds(RiskThresholds thresholds)
    {
        RiskThresholds = new Dictionary<RiskLevel, double>
        {
            { RiskLevel.Low, thresholds.LowRisk },
            { RiskLevel.Medium, thresholds.MediumRisk },
            { RiskLevel.High, thresholds.HighRisk },
            { RiskLevel.Critical, thresholds.CriticalRisk }
        };
    }
}