namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Analiz sonucu DTO
/// </summary>
public class AnalysisResultDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public double FraudProbability { get; set; }
    public double AnomalyScore { get; set; }
    public string RiskLevel { get; set; }
    public string Decision { get; set; }
    public List<RiskFactorDto> RiskFactors { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}