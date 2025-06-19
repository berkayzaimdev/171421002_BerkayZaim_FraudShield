namespace Analiz.Domain.Entities.RuleConfigurations;

/// <summary>
/// IP Tabanlı kural konfigürasyonu
/// </summary>
public class IpBasedRuleConfig
{
    /// <summary>
    /// Engellenecek IP adresleri listesi
    /// </summary>
    public List<string> BlockedIps { get; set; }

    /// <summary>
    /// Engellenecek IP aralıkları (CIDR notasyonu)
    /// </summary>
    public List<string> BlockedIpRanges { get; set; }

    /// <summary>
    /// Engellenecek ülke kodları
    /// </summary>
    public List<string> BlockedCountries { get; set; }

    /// <summary>
    /// Şüpheli ağ tipleri
    /// </summary>
    public List<string> SuspiciousNetworks { get; set; }

    /// <summary>
    /// Bu zaman dilimi içinde (saniye)
    /// </summary>
    public int TimeWindowSeconds { get; set; }

    /// <summary>
    /// Maksimum izin verilen farklı hesap sayısı
    /// </summary>
    public int MaxDifferentAccounts { get; set; }

    /// <summary>
    /// Maksimum izin verilen başarısız giriş sayısı
    /// </summary>
    public int MaxFailedLogins { get; set; }
}