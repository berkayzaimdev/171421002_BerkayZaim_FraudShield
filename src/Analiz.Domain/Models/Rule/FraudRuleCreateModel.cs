using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Domain.Entities.Rule;

/// <summary>
/// Kural oluşturma modeli
/// </summary>
public class FraudRuleCreateModel
{
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
    /// Kural aksiyonları
    /// </summary>
    public List<RuleAction> Actions { get; set; }

    /// <summary>
    /// Aksiyon süresi (null ise süresiz)
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