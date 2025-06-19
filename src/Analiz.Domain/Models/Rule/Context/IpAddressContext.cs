namespace Analiz.Domain.Entities.Rule.Context;

/// <summary>
/// IP adresi bağlamı
/// </summary>
public class IpAddressContext
{
    /// <summary>
    /// IP adresi
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// Ülke kodu
    /// </summary>
    public string CountryCode { get; set; }

    /// <summary>
    /// Şehir
    /// </summary>
    public string City { get; set; }

    /// <summary>
    /// ISP/ASN bilgisi
    /// </summary>
    public string IspAsn { get; set; }

    /// <summary>
    /// Reputation puanı (düşük değerler riskli)
    /// </summary>
    public int ReputationScore { get; set; }

    /// <summary>
    /// Kara listede mi?
    /// </summary>
    public bool IsBlacklisted { get; set; }

    /// <summary>
    /// Kara liste notları
    /// </summary>
    public string BlacklistNotes { get; set; }

    /// <summary>
    /// Datacenter/proxy/VPN/Tor mu?
    /// </summary>
    public bool IsDatacenterOrProxy { get; set; }

    /// <summary>
    /// Ağ tipi
    /// </summary>
    public string NetworkType { get; set; }

    /// <summary>
    /// Son 10 dakikadaki bağlanan farklı hesap sayısı
    /// </summary>
    public int UniqueAccountCount10m { get; set; }

    /// <summary>
    /// Son 1 saatteki bağlanan farklı hesap sayısı
    /// </summary>
    public int UniqueAccountCount1h { get; set; }

    /// <summary>
    /// Son 24 saatteki bağlanan farklı hesap sayısı
    /// </summary>
    public int UniqueAccountCount24h { get; set; }

    /// <summary>
    /// Son 10 dakikadaki başarısız giriş sayısı
    /// </summary>
    public int FailedLoginCount10m { get; set; }

    /// <summary>
    /// IP ek verileri
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}