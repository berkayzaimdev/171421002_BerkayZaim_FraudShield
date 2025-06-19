namespace Analiz.Domain.Entities.RuleConfigurations;

/// <summary>
/// Hesap Tabanlı kural konfigürasyonu
/// </summary>
public class AccountBasedRuleConfig
{
    /// <summary>
    /// Bu zaman dilimi içinde (saniye)
    /// </summary>
    public int TimeWindowSeconds { get; set; }

    /// <summary>
    /// Maksimum izin verilen farklı IP sayısı
    /// </summary>
    public int MaxDifferentIps { get; set; }

    /// <summary>
    /// Maksimum izin verilen farklı cihaz sayısı
    /// </summary>
    public int MaxDifferentDevices { get; set; }

    /// <summary>
    /// Maksimum izin verilen farklı ülke sayısı
    /// </summary>
    public int MaxDifferentCountries { get; set; }

    /// <summary>
    /// Maksimum izin verilen oturum süresi (dakika)
    /// </summary>
    public int MaxSessionDurationMinutes { get; set; }

    /// <summary>
    /// Maksimum izin verilen transfer sayısı
    /// </summary>
    public int MaxTransfers { get; set; }

    /// <summary>
    /// Maksimum izin verilen farklı alıcı sayısı
    /// </summary>
    public int MaxDifferentRecipients { get; set; }
}