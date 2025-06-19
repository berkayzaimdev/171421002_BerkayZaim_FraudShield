namespace FraudShield.TransactionAnalysis.Domain.Enums.Rule;

/// <summary>
/// Kural durumları
/// </summary>
public enum RuleStatus
{
    /// <summary>
    /// Taslak - henüz uygulanmıyor
    /// </summary>
    Draft,

    /// <summary>
    /// Aktif - kurallar uygulanıyor
    /// </summary>
    Active,

    /// <summary>
    /// Pasif - kurallar devre dışı
    /// </summary>
    Inactive,

    /// <summary>
    /// Test Modu - kurallar tetikleniyor ama aksiyon alınmıyor
    /// </summary>
    TestMode,

    /// <summary>
    /// Arşivlenmiş - artık kullanılmıyor
    /// </summary>
    Archived
}