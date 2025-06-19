using System.ComponentModel.DataAnnotations;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.DTOs.Request;

/// <summary>
/// Fraud olayı çözme isteği
/// </summary>
public class FraudEventResolveRequest
{
    /// <summary>
    /// Olay durumu
    /// </summary>
    [Required]
    public FraudEventStatus Status { get; set; }

    /// <summary>
    /// Çözüm notları
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string ResolutionNotes { get; set; }
}