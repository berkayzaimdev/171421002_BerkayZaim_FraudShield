using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Fraud tespiti sonucu
/// </summary>
public class FraudDetectionResult
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
    /// Risk puanı (0-100 arası)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Tetiklenen kural sonuçları
    /// </summary>
    public List<RuleEvaluationResult> TriggeredRules { get; set; }

    /// <summary>
    /// Oluşturulan olaylar
    /// </summary>
    public List<Guid> CreatedEventIds { get; set; }

    /// <summary>
    /// Sonuç mesajı
    /// </summary>
    public string ResultMessage { get; set; }

    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccess => ResultType == FraudDetectionResultType.Approved;

    /// <summary>
    /// Aksiyon gerektiriyor mu?
    /// </summary>
    public bool RequiresAction => ResultType != FraudDetectionResultType.Approved;
}