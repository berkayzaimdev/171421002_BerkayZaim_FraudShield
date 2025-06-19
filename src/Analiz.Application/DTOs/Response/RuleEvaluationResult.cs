using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Kural değerlendirme sonucu
/// </summary>
public class RuleEvaluationResult
{
    /// <summary>
    /// Kural ID'si
    /// </summary>
    public Guid RuleId { get; set; }

    /// <summary>
    /// Kural kodu
    /// </summary>
    public string RuleCode { get; set; }

    /// <summary>
    /// Kural adı
    /// </summary>
    public string RuleName { get; set; }

    /// <summary>
    /// Kural tetiklendi mi?
    /// </summary>
    public bool IsTriggered { get; set; }

    /// <summary>
    /// Tetiklenme skoru (0-1 arası)
    /// </summary>
    public double TriggerScore { get; set; }

    /// <summary>
    /// Alınan aksiyonlar
    /// </summary>
    public List<RuleAction> Actions { get; set; }

    /// <summary>
    /// Aksiyon süresi
    /// </summary>
    public TimeSpan? ActionDuration { get; set; }

    /// <summary>
    /// Tetiklenme detayları
    /// </summary>
    public string TriggerDetails { get; set; }

    /// <summary>
    /// İlgili bir olay oluşturuldu mu?
    /// </summary>
    public bool EventCreated { get; set; }

    /// <summary>
    /// Oluşturulan olayın ID'si
    /// </summary>
    public Guid? EventId { get; set; }
}