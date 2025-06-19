using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Request;

/// <summary>
/// IP kontrolü isteği
/// </summary>
public class IpCheckRequest
{
    /// <summary>
    /// IP adresi
    /// </summary>
    [Required]
    public string IpAddress { get; set; }

    /// <summary>
    /// Ülke kodu
    /// </summary>
    [StringLength(2, MinimumLength = 2)]
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
    /// İtibar puanı
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
    /// Datacenter/proxy mu?
    /// </summary>
    public bool IsDatacenterOrProxy { get; set; }

    /// <summary>
    /// Ağ tipi
    /// </summary>
    public string NetworkType { get; set; }

    /// <summary>
    /// Son 10 dakikadaki farklı hesap sayısı
    /// </summary>
    public int UniqueAccountCount10m { get; set; }

    /// <summary>
    /// Son 1 saatteki farklı hesap sayısı
    /// </summary>
    public int UniqueAccountCount1h { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı hesap sayısı
    /// </summary>
    public int UniqueAccountCount24h { get; set; }

    /// <summary>
    /// Son 10 dakikadaki başarısız giriş sayısı
    /// </summary>
    public int FailedLoginCount10m { get; set; }

    /// <summary>
    /// Ek veriler
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}