using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Dolandırıcılık alarmı DTO
/// </summary>
public class FraudAlertDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public AlertType Type { get; set; }
    public AlertStatus Status { get; set; }
    public string RiskLevel { get; set; }
    public List<string> Factors { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}