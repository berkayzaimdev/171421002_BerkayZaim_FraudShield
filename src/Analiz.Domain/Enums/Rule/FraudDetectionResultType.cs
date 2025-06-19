namespace FraudShield.TransactionAnalysis.Domain.Enums.Rule;

/// <summary>
/// Fraud tespiti sonuç türleri
/// </summary>
public enum FraudDetectionResultType
{
    /// <summary>
    /// Onaylandı
    /// </summary>
    Approved,

    /// <summary>
    /// İlave doğrulama gerekli
    /// </summary>
    AdditionalVerificationRequired,

    /// <summary>
    /// İncelemeye alındı
    /// </summary>
    ReviewRequired,

    /// <summary>
    /// Geçici olarak engellendi
    /// </summary>
    TemporarilyBlocked,

    /// <summary>
    /// Kalıcı olarak engellendi
    /// </summary>
    PermanentlyBlocked,

    /// <summary>
    /// Reddedildi
    /// </summary>
    Rejected,
    Error
}