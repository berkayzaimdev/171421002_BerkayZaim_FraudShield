using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

/// <summary>
/// Kara liste öğesi
/// </summary>
public class BlacklistItem : Entity
{
    /// <summary>
    /// Kara liste tipi (IP, Hesap, Cihaz, Ülke)
    /// </summary>
    public BlacklistType Type { get; private set; }

    /// <summary>
    /// Kara listeye alınan değer (IP, hesap ID, cihaz ID, ülke kodu)
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Açıklama/neden
    /// </summary>
    public string Reason { get; private set; }

    /// <summary>
    /// Kaynaklandığı kural ID'si (varsa)
    /// </summary>
    public Guid? RuleId { get; private set; }

    /// <summary>
    /// Eklenen olayın ID'si (varsa)
    /// </summary>
    public Guid? EventId { get; private set; }

    /// <summary>
    /// Bitiş tarihi (null ise süresiz)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Kara liste durumu
    /// </summary>
    public BlacklistStatus Status { get; set; }

    /// <summary>
    /// Ekleyen kullanıcı/sistem
    /// </summary>
    public string AddedBy { get; private set; }

    /// <summary>
    /// Geçersiz kılan kullanıcı/sistem
    /// </summary>
    public string InvalidatedBy { get; private set; }

    /// <summary>
    /// Geçersiz kılma tarihi
    /// </summary>
    public DateTime? InvalidatedAt { get; private set; }

    /// <summary>
    /// Kara liste öğesi oluşturma factory metodu
    /// </summary>
    public static BlacklistItem Create(
        BlacklistType type,
        string value,
        string reason,
        Guid? ruleId = null,
        Guid? eventId = null,
        TimeSpan? duration = null,
        string addedBy = "system")
    {
        var item = new BlacklistItem
        {
            Id = Guid.NewGuid(),
            Type = type,
            Value = value,
            Reason = reason,
            RuleId = ruleId,
            EventId = eventId,
            ExpiryDate = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null,
            Status = BlacklistStatus.Active,
            AddedBy = addedBy,
            CreatedAt = DateTime.UtcNow
        };

        return item;
    }

    /// <summary>
    /// Kara liste öğesini geçersiz kıl
    /// </summary>
    public void Invalidate(string invalidatedBy)
    {
        Status = BlacklistStatus.Invalidated;
        InvalidatedBy = invalidatedBy;
        InvalidatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Süresinin dolup dolmadığını kontrol et
    /// </summary>
    public bool IsExpired()
    {
        return ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// Aktif mi kontrol et
    /// </summary>
    public bool IsActive()
    {
        return Status == BlacklistStatus.Active && !IsExpired();
    }
}