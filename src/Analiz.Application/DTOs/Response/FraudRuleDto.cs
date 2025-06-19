using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Dolandırıcılık kuralı DTO
/// </summary>
public class FraudRuleDto
{
    public Guid Id { get; set; }
    public string RuleId { get; set; }

    [Required] public string Name { get; set; }

    [Required] public string Description { get; set; }

    [Required] public string RuleType { get; set; } // Simple veya Complex

    // Simple kural için gerekli alanlar
    public string Field { get; set; }
    public string Operator { get; set; }
    public string Value { get; set; }

    // Complex kural için gerekli alan
    public string Condition { get; set; }

    [Required] public string Action { get; set; } // Block, Review, Alert, None

    public decimal Threshold { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
    public int Score { get; set; }

    [Required] public string Priority { get; set; } // High, Medium, Low

    public string RuleGroup { get; set; } = "DEFAULT";

    public bool IsActive { get; set; } = true;

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }
}