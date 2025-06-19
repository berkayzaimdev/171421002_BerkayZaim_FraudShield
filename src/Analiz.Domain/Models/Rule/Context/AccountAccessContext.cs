namespace Analiz.Domain.Entities.Rule.Context;

/// <summary>
/// Hesap erişim bağlamı
/// </summary>
public class AccountAccessContext
{
    /// <summary>
    /// Hesap ID'si
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Erişim tarihi
    /// </summary>
    public DateTime AccessDate { get; set; }

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
    /// Cihaz ID'si
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Cihaz güvenilir mi?
    /// </summary>
    public bool IsTrustedDevice { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı IP adresi sayısı
    /// </summary>
    public int UniqueIpCount24h { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı ülke sayısı
    /// </summary>
    public int UniqueCountryCount24h { get; set; }

    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Son başarısız giriş sayısı
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Kayıtlı olan tipik erişim saatleri
    /// </summary>
    public List<int> TypicalAccessHours { get; set; }

    /// <summary>
    /// Kayıtlı olan tipik erişim günleri
    /// </summary>
    public List<DayOfWeek> TypicalAccessDays { get; set; }

    /// <summary>
    /// Kayıtlı olan tipik erişim ülkeleri
    /// </summary>
    public List<string> TypicalCountries { get; set; }

    /// <summary>
    /// Hesap ek verileri
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}