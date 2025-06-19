using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Models.Configuration;

/// <summary>
/// Configuration class for amount adjustment algorithm
/// </summary>
public class AmountAdjustmentConfiguration
{
    // Amount tier definitions
    public AmountTier ExtremeTier { get; set; } = new AmountTier
    {
        Threshold = 1_000_000m,
        BaseValue = 0.75,
        ScaleFactor = 0.25,
        WeightFactor = 1.5,
        RiskMultiplier = 1.3,
        MinRiskLevel = RiskLevel.Medium
    };

    public AmountTier VeryHighTier { get; set; } = new AmountTier
    {
        Threshold = 500_000m,
        BaseValue = 0.70,
        ScaleFactor = 0.25,
        WeightFactor = 1.3,
        RiskMultiplier = 1.2
    };

    // Additional tiers omitted for brevity

    // Global parameters
    public double MinProbability { get; set; } = 0.01;
    public double MaxProbability { get; set; } = 0.98;
    public double ProbabilityExponent { get; set; } = 1.5;
    public double LogScalingFactor { get; set; } = 0.4;

    // Sector-specific adjustment maps
    public Dictionary<string, double> SectorRiskMultipliers { get; set; } =
        new Dictionary<string, double>
        {
            ["HighRisk"] = 1.2,
            ["MediumRisk"] = 1.0,
            ["LowRisk"] = 0.8
        };
}

/// <summary>
/// Defines parameters for an amount tier
/// </summary>
public class AmountTier
{
    public decimal Threshold { get; set; }
    public double BaseValue { get; set; }
    public double ScaleFactor { get; set; }
    public double WeightFactor { get; set; }
    public double RiskMultiplier { get; set; } = 1.0;
    public RiskLevel MinRiskLevel { get; set; } = RiskLevel.Low;
}