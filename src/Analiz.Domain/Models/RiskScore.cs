using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class RiskScore
{
    public double Score { get; private set; }
    public RiskLevel Level { get; private set; }
    public List<string> Factors { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    public RiskScore(double score, List<string> factors)
    {
        Score = score;
        Factors = factors;
        CalculatedAt = DateTime.UtcNow;
        Level = DetermineRiskLevel(score);
    }

    public static RiskScore Create(double score, List<string> factors)
    {
        if (score < 0 || score > 1)
            throw new ArgumentOutOfRangeException(nameof(score), "Risk score must be between 0 and 1");

        return new RiskScore(score, factors ?? new List<string>());
    }

    private static RiskLevel DetermineRiskLevel(double score)
    {
        return score switch
        {
            >= 0.9 => RiskLevel.Critical,
            >= 0.75 => RiskLevel.High,
            >= 0.5 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }
}