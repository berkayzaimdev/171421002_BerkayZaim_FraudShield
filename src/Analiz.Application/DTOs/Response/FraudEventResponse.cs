using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Fraud olayı yanıtı
/// </summary>
public class FraudEventResponse
{
    /// <summary>
    /// Olay ID'si
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// İlgili kural ID'si
    /// </summary>
    public Guid RuleId { get; set; }

    /// <summary>
    /// Kural adı
    /// </summary>
    public string RuleName { get; set; }

    /// <summary>
    /// Kural kodu
    /// </summary>
    public string RuleCode { get; set; }

    /// <summary>
    /// İlgili işlem ID'si
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// İlgili hesap ID'si
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// İlgili IP adresi
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// İlgili cihaz bilgisi
    /// </summary>
    public string DeviceInfo { get; set; }

    /// <summary>
    /// Alınan aksiyonlar
    /// </summary>
    public List<RuleAction> Actions { get; set; }

    /// <summary>
    /// Aksiyon süresi
    /// </summary>
    public TimeSpan? ActionDuration { get; set; }

    /// <summary>
    /// Aksiyon bitiş tarihi
    /// </summary>
    public DateTime? ActionEndDate { get; set; }

    /// <summary>
    /// Olay detayları JSON
    /// </summary>
    public string EventDetailsJson { get; set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Çözülme tarihi
    /// </summary>
    public DateTime? ResolvedDate { get; set; }

    /// <summary>
    /// Çözen kişi/sistem
    /// </summary>
    public string ResolvedBy { get; set; }

    /// <summary>
    /// Çözüm notları
    /// </summary>
    public string ResolutionNotes { get; set; }
}