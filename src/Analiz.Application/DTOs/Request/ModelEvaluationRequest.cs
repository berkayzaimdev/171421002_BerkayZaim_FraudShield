using System.ComponentModel.DataAnnotations;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.DTOs.Request;

/// <summary>
/// ML modeli değerlendirme isteği
/// </summary>
public class ModelEvaluationRequest
{
    /// <summary>
    /// İşlem ID'si
    /// </summary>
    [Required]
    public Guid TransactionId { get; set; }

    /// <summary>
    /// İşlem tutarı
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// İşlem tarihi
    /// </summary>
    [Required]
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// İşlem tipi
    /// </summary>
    [Required]
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// V1-V28 değerleri ve diğer özellikler
    /// </summary>
    public Dictionary<string, string> Features { get; set; }

    /// <summary>
    /// Ek veriler
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}