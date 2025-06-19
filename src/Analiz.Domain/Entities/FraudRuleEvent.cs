using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Domain.Entities;

/// <summary>
/// Kuralın aktif olarak tetiklendiği fraud olayı
/// </summary>
public class FraudRuleEvent : Entity
{
    /// <summary>
    /// İlgili kuralın ID'si
    /// </summary>
    public Guid RuleId { get; private set; }

    /// <summary>
    /// Kuralın adı
    /// </summary>
    public string RuleName { get; private set; }

    /// <summary>
    /// Kuralın kodu
    /// </summary>
    public string RuleCode { get; private set; }

    /// <summary>
    /// Tetikleyen işlemin ID'si
    /// </summary>
    public Guid? TransactionId { get; private set; }

    /// <summary>
    /// Tetikleyen hesabın ID'si
    /// </summary>
    public Guid? AccountId { get; private set; }

    /// <summary>
    /// Tetikleyen IP adresi
    /// </summary>
    public string IpAddress { get; private set; }

    /// <summary>
    /// Tetikleyen cihaz bilgisi
    /// </summary>
    public string DeviceInfo { get; private set; }

    /// <summary>
    /// Olay sonucunda alınan aksiyonlar
    /// </summary>
    public List<RuleAction> Actions { get; private set; }

    /// <summary>
    /// Aksiyon süresi (null ise süresiz)
    /// </summary>
    public TimeSpan? ActionDuration { get; private set; }

    /// <summary>
    /// Aksiyon bitiş tarihi (süre varsa)
    /// </summary>
    public DateTime? ActionEndDate { get; private set; }

    /// <summary>
    /// Olay detayları (JSON formatında)
    /// </summary>
    public string EventDetailsJson { get; private set; }

    /// <summary>
    /// Olayın çözüldüğü/kapatıldığı tarih
    /// </summary>
    public DateTime? ResolvedDate { get; private set; }

    /// <summary>
    /// Olayı çözen/kapatan kişi/sistem
    /// </summary>
    public string ResolvedBy { get; private set; }

    /// <summary>
    /// Çözüm açıklaması
    /// </summary>
    public string ResolutionNotes { get; private set; }

    /// <summary>
    /// Fraud olayı oluşturma factory metodu
    /// </summary>
    public static FraudRuleEvent Create(
        Guid ruleId,
        string ruleName,
        string ruleCode,
        Guid? transactionId,
        Guid? accountId,
        string ipAddress,
        string deviceInfo,
        List<RuleAction> actions,
        TimeSpan? actionDuration,
        string eventDetailsJson)
    {
        var ruleEvent = new FraudRuleEvent
        {
            Id = Guid.NewGuid(),
            RuleId = ruleId,
            RuleName = ruleName,
            RuleCode = ruleCode,
            TransactionId = transactionId,
            AccountId = accountId,
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo,
            Actions = actions ?? new List<RuleAction>(),
            ActionDuration = actionDuration,
            ActionEndDate = actionDuration.HasValue ? DateTime.Now.Add(actionDuration.Value) : null,
            EventDetailsJson = eventDetailsJson,
            CreatedAt = DateTime.Now
        };

        return ruleEvent;
    }

    /// <summary>
    /// Fraud olayını çöz/kapat
    /// </summary>
    public void Resolve(string resolvedBy, string resolutionNotes)
    {
        ResolvedDate = DateTime.Now;
        ResolvedBy = resolvedBy;
        ResolutionNotes = resolutionNotes;
    }
}