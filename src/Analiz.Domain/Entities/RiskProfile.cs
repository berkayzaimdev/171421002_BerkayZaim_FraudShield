namespace Analiz.Domain;

public class RiskProfile
{
    public Guid UserId { get; set; }
    public double AverageRiskScore { get; set; }
    public int TransactionCount { get; set; }
    public int HighRiskTransactionCount { get; set; }
    public Dictionary<string, int> CommonRiskFactors { get; set; }
    public DateTime LastUpdated { get; set; }
    public double AverageTransactionAmount { get; set; }
}