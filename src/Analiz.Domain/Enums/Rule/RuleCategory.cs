namespace FraudShield.TransactionAnalysis.Domain.Enums.Rule;

/// <summary>
/// Kural kategorileri
/// </summary>
public enum RuleCategory
{
    /// <summary>
    /// Ağ tabanlı kurallar (Tor, VPN, proxy vb.)
    /// </summary>
    Network,

    /// <summary>
    /// IP tabanlı kurallar
    /// </summary>
    IP,

    /// <summary>
    /// Hesap tabanlı kurallar
    /// </summary>
    Account,

    /// <summary>
    /// Cihaz tabanlı kurallar
    /// </summary>
    Device,

    /// <summary>
    /// Oturum tabanlı kurallar
    /// </summary>
    Session,

    /// <summary>
    /// İşlem tabanlı kurallar
    /// </summary>
    Transaction,

    /// <summary>
    /// Davranış tabanlı kurallar
    /// </summary>
    Behavior,

    /// <summary>
    /// Zaman tabanlı kurallar
    /// </summary>
    Time,

    /// <summary>
    /// Lokasyon tabanlı kurallar
    /// </summary>
    Location,

    /// <summary>
    /// Diğer kurallar
    /// </summary>
    Other,
    Complex
}