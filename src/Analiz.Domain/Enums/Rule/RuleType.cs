namespace FraudShield.TransactionAnalysis.Domain.Enums.Rule;

/// <summary>
/// Kural tipleri
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Basit kural (tek bir koşul)
    /// </summary>
    Simple,

    /// <summary>
    /// Eşik tabanlı kural (bir sayaç ve eşik değeri)
    /// </summary>
    Threshold,

    /// <summary>
    /// Karmaşık kural (birden fazla koşul kombinasyonu)
    /// </summary>
    Complex,

    /// <summary>
    /// Sıralı kural (belirli bir sırada gerçekleşen olaylar)
    /// </summary>
    Sequential,

    /// <summary>
    /// Davranışsal kural (kullanıcı davranışı modeline dayalı)
    /// </summary>
    Behavioral,

    /// <summary>
    /// Anomali tabanlı kural (normal davranıştan sapma)
    /// </summary>
    Anomaly,

    /// <summary>
    /// Blacklist kural (kara liste tabanlı)
    /// </summary>
    Blacklist,

    /// <summary>
    /// Whitelist kural (beyaz liste tabanlı)
    /// </summary>
    Whitelist,

    /// <summary>
    /// Özel kural (özel mantık veya kod içeren)
    /// </summary>
    Custom
}