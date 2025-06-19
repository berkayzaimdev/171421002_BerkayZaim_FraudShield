using System.ComponentModel.DataAnnotations;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.DTOs.Request;

/// <summary>
/// Fraud kuralı oluşturma isteği
/// </summary>
public class FraudRuleCreateRequest
{
    /// <summary>
    /// Kural adı
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    /// <summary>
    /// Kural açıklaması
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// Kural kategorisi
    /// </summary>
    [Required]
    public RuleCategory Category { get; set; }

    /// <summary>
    /// Kural tipi
    /// </summary>
    [Required]
    public RuleType Type { get; set; }

    /// <summary>
    /// Kural etki seviyesi
    /// </summary>
    [Required]
    public ImpactLevel ImpactLevel { get; set; }

    /// <summary>
    /// Kural aksiyonları
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<RuleAction> Actions { get; set; }

    /// <summary>
    /// Aksiyon süresi (null ise süresiz)
    /// </summary>
    public TimeSpan? ActionDuration { get; set; }

    /// <summary>
    /// Kural önceliği
    /// </summary>
    [Range(1, 100)]
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Kural koşul ifadesi
    /// </summary>
    public string Condition { get; set; }

    /// <summary>
    /// Kural konfigürasyonu
    /// </summary>
    public object Configuration { get; set; }

    /// <summary>
    /// Geçerlilik başlangıç tarihi
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Geçerlilik bitiş tarihi
    /// </summary>
    public DateTime? ValidTo { get; set; }
}