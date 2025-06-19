using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Request;

/// <summary>
/// Cihaz kontrolü isteği
/// </summary>
public class DeviceCheckRequest
{
    /// <summary>
    /// Cihaz ID'si
    /// </summary>
    [Required]
    public string DeviceId { get; set; }

    /// <summary>
    /// Cihaz tipi
    /// </summary>
    public string DeviceType { get; set; }

    /// <summary>
    /// İşletim sistemi
    /// </summary>
    public string OperatingSystem { get; set; }

    /// <summary>
    /// Tarayıcı
    /// </summary>
    public string Browser { get; set; }

    /// <summary>
    /// IP adresi
    /// </summary>
    [Required]
    public string IpAddress { get; set; }

    /// <summary>
    /// Ülke kodu
    /// </summary>
    public string CountryCode { get; set; }


    /// <summary>
    /// Emulator mu?
    /// </summary>
    public bool IsEmulator { get; set; }

    /// <summary>
    /// Jailbreak yapılmış mı?
    /// </summary>
    public bool IsJailbroken { get; set; }

    /// <summary>
    /// Root yapılmış mı?
    /// </summary>
    public bool IsRooted { get; set; }


    /// <summary>
    /// İlk görülme tarihi
    /// </summary>
    public DateTime? FirstSeenDate { get; set; }

    /// <summary>
    /// Son görülme tarihi
    /// </summary>
    public DateTime? LastSeenDate { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı hesap sayısı
    /// </summary>
    public int UniqueAccountCount24h { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı IP sayısı
    /// </summary>
    public int UniqueIpCount24h { get; set; }

    /// <summary>
    /// Ek veriler
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}