namespace FraudShield.TransactionAnalysis.Domain.Enums.Rule;

/// <summary>
/// Fraud olay durumları
/// </summary>
public enum FraudEventStatus
{
    /// <summary>
    /// Yeni tespit edilmiş
    /// </summary>
    New,

    /// <summary>
    /// İnceleme altında
    /// </summary>
    UnderInvestigation,

    /// <summary>
    /// Çözüldü - Gerçek Fraud
    /// </summary>
    ResolvedFraud,

    /// <summary>
    /// Çözüldü - Yanlış Alarm
    /// </summary>
    ResolvedFalsePositive,

    /// <summary>
    /// Çözüldü - Bulanık (kesin değil)
    /// </summary>
    ResolvedIndeterminate,

    /// <summary>
    /// Kapatıldı - Aksiyon alınmadı
    /// </summary>
    ClosedNoAction
}