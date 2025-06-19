using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Fraud kuralı yanıtı
/// </summary>
public class FraudRuleResponse
{
    /// <summary>
    /// Kural ID'si
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Kural kodu
    /// </summary>
    public string RuleCode { get; set; }

    /// <summary>
    /// Kural adı
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Kural açıklaması
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Kural kategorisi
    /// </summary>
    public RuleCategory Category { get; set; }

    /// <summary>
    /// Kural tipi
    /// </summary>
    public RuleType Type { get; set; }

    /// <summary>
    /// Kural etki seviyesi
    /// </summary>
    public ImpactLevel ImpactLevel { get; set; }

    /// <summary>
    /// Kural durumu
    /// </summary>
    public RuleStatus Status { get; set; }

    /// <summary>
    /// Kural aksiyonları
    /// </summary>
    public List<RuleAction> Actions { get; set; }

    /// <summary>
    /// Aksiyon süresi
    /// </summary>
    public TimeSpan? ActionDuration { get; set; }

    /// <summary>
    /// Kural önceliği
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Kural koşul ifadesi
    /// </summary>
    public string Condition { get; set; }

    /// <summary>
    /// Kural konfigürasyonu JSON
    /// </summary>
    public string ConfigurationJson { get; set; }

    /// <summary>
    /// Geçerlilik başlangıç tarihi
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Geçerlilik bitiş tarihi
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Son değiştirilme tarihi
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Değiştiren kişi/sistem
    /// </summary>
    public string ModifiedBy { get; set; }
}