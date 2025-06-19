using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities.Rule.Context;

/// <summary>
/// İşlem bağlamı
/// </summary>
public class TransactionContext
{
    /// <summary>
    /// İşlem ID'si
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Hesap ID'si
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// İşlem tutarı
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// İşlem para birimi
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// İşlem tipi
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// İşlem tarihi
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Alıcı hesap ID'si
    /// </summary>
    public Guid? RecipientAccountId { get; set; }

    /// <summary>
    /// Alıcı hesap adı/numarası
    /// </summary>
    public string RecipientAccountNumber { get; set; }

    /// <summary>
    /// Alıcı ülkesi
    /// </summary>
    public string RecipientCountry { get; set; }

    /// <summary>
    /// Kullanıcının son 24 saatteki işlem sayısı
    /// </summary>
    public int UserTransactionCount24h { get; set; }

    /// <summary>
    /// Kullanıcının son 24 saatteki toplam işlem tutarı
    /// </summary>
    public decimal UserTotalAmount24h { get; set; }

    /// <summary>
    /// Kullanıcının ortalama işlem tutarı
    /// </summary>
    public decimal UserAverageTransactionAmount { get; set; }

    /// <summary>
    /// Kullanıcının ilk işleminden bu yana geçen gün sayısı
    /// </summary>
    public int DaysSinceFirstTransaction { get; set; }

    /// <summary>
    /// Son 1 saatte farklı alıcı sayısı
    /// </summary>
    public int UniqueRecipientCount1h { get; set; }

    /// <summary>
    /// İşlem ek verileri
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}