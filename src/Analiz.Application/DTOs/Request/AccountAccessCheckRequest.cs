using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Request;

/// <summary>
/// Hesap erişimi kontrolü isteği
/// </summary>
public class AccountAccessCheckRequest
{
    /// <summary>
    /// Hesap ID'si
    /// </summary>
    [Required]
    public Guid AccountId { get; set; }

    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    [Required]
    public string Username { get; set; }

    /// <summary>
    /// Erişim tarihi
    /// </summary>
    [Required]
    public DateTime AccessDate { get; set; }

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
    /// Cihaz ID'si
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Güvenilir cihaz mı?
    /// </summary>
    public bool IsTrustedDevice { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı IP sayısı
    /// </summary>
    public int UniqueIpCount24h { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı ülke sayısı
    /// </summary>
    public int UniqueCountryCount24h { get; set; }

    /// <summary>
    /// Başarılı giriş mi?
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Son başarısız giriş sayısı
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Tipik erişim saatleri
    /// </summary>
    public List<int> TypicalAccessHours { get; set; }

    /// <summary>
    /// Tipik erişim günleri
    /// </summary>
    public List<string> TypicalAccessDays { get; set; }

    /// <summary>
    /// Tipik erişim ülkeleri
    /// </summary>
    public List<string> TypicalCountries { get; set; }

    /// <summary>
    /// Ek veriler
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}