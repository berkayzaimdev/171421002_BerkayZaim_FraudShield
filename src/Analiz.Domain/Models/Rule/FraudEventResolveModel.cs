using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Domain.Models.Rule;

/// <summary>
/// Olay çözme modeli
/// </summary>
public class FraudEventResolveModel
{
    /// <summary>
    /// Çözüm durumu
    /// </summary>
    public FraudEventStatus Status { get; set; }

    /// <summary>
    /// Çözüm notları
    /// </summary>
    public string ResolutionNotes { get; set; }
}