namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Risk faktörü DTO
/// </summary>
public class RiskFactorDto
{
    public string Type { get; set; }
    public string Description { get; set; }
    public double Confidence { get; set; }
}