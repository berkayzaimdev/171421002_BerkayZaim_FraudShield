namespace FraudShield.TransactionAnalysis.Domain.Enums.Rule;

/// <summary>
/// Kural etki seviyeleri
/// </summary>
public enum ImpactLevel
{
    /// <summary>
    /// Düşük etki - sadece bildirim, loglama vb.
    /// </summary>
    Low,

    /// <summary>
    /// Orta etki - ilave doğrulama isteği, limitleme vb.
    /// </summary>
    Medium,

    /// <summary>
    /// Yüksek etki - hesap kısıtlama, işlem engelleme vb.
    /// </summary>
    High,

    /// <summary>
    /// Kritik etki - hesap bloke, IP engelleme vb.
    /// </summary>
    Critical
}