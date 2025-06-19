using System.ComponentModel.DataAnnotations;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.DTOs.Request;

/// <summary>
/// İşlem kontrolü isteği
/// </summary>
public class TransactionCheckRequest
{
    /// <summary>
    /// İşlem ID'si
    /// </summary>
    [Required]
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Hesap ID'si
    /// </summary>
    [Required]
    public Guid AccountId { get; set; }

    /// <summary>
    /// İşlem tutarı
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Para birimi
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; }

    /// <summary>
    /// İşlem tipi
    /// </summary>
    [Required]
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// İşlem tarihi
    /// </summary>
    [Required]
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Alıcı hesap ID'si
    /// </summary>
    public Guid? RecipientAccountId { get; set; }

    /// <summary>
    /// Alıcı hesap numarası
    /// </summary>
    public string RecipientAccountNumber { get; set; }

    /// <summary>
    /// Alıcı ülkesi
    /// </summary>
    public string RecipientCountry { get; set; }

    /// <summary>
    /// Son 24 saatteki işlem sayısı
    /// </summary>
    public int UserTransactionCount24h { get; set; }

    /// <summary>
    /// Son 24 saatteki toplam işlem tutarı
    /// </summary>
    public decimal UserTotalAmount24h { get; set; }

    /// <summary>
    /// Ortalama işlem tutarı
    /// </summary>
    public decimal UserAverageTransactionAmount { get; set; }

    /// <summary>
    /// İlk işlemden bu yana geçen gün sayısı
    /// </summary>
    public int DaysSinceFirstTransaction { get; set; }

    /// <summary>
    /// Son 1 saatteki farklı alıcı sayısı
    /// </summary>
    public int UniqueRecipientCount1h { get; set; }

    /// <summary>
    /// Ek veriler
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}