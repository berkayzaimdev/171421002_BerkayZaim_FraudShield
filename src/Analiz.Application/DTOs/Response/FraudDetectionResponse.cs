using Analiz.Domain.Models;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Fraud tespiti yanıtı
/// </summary>
public class FraudDetectionResponse
{
    /// <summary>
    /// Sonuç türü
    /// </summary>
    public FraudDetectionResultType ResultType { get; set; }

    /// <summary>
    /// Alınan aksiyonlar
    /// </summary>
    public List<RuleAction> Actions { get; set; }

    /// <summary>
    /// Aksiyon süresi
    /// </summary>
    public TimeSpan? ActionDuration { get; set; }

    /// <summary>
    /// Risk puanı
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Tetiklenen kural sayısı
    /// </summary>
    public int TriggeredRuleCount { get; set; }

    /// <summary>
    /// Tetiklenen kurallar
    /// </summary>
    public List<TriggeredRuleInfo> TriggeredRules { get; set; }

    /// <summary>
    /// Oluşturulan olayların ID'leri
    /// </summary>
    public List<Guid> CreatedEventIds { get; set; }

    /// <summary>
    /// Sonuç mesajı
    /// </summary>
    public string ResultMessage { get; set; }

    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Aksiyon gerekli mi?
    /// </summary>
    public bool RequiresAction { get; set; }
}