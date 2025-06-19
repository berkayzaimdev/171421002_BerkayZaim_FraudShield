namespace FraudShield.TransactionAnalysis.Domain.Enums;

/// <summary>
/// Kara liste durumu
/// </summary>
public enum BlacklistStatus
{
    /// <summary>
    /// Aktif
    /// </summary>
    Active,

    /// <summary>
    /// Geçersiz kılınmış (manuel)
    /// </summary>
    Invalidated,

    /// <summary>
    /// Süresi dolmuş
    /// </summary>
    Expired
}