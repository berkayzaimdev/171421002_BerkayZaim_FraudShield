namespace Analiz.Domain.Entities.Rule.Context;

/// <summary>
/// Oturum bağlamı
/// </summary>
public class SessionContext
{
    /// <summary>
    /// Oturum ID'si
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Hesap ID'si
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Oturum başlangıç tarihi
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Son aktivite tarihi
    /// </summary>
    public DateTime LastActivityTime { get; set; }

    /// <summary>
    /// Aktif kalma süresi (dakika)
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// IP adresi
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// Cihaz ID'si
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// User Agent bilgisi
    /// </summary>
    public string UserAgent { get; set; }

    /// <summary>
    /// Erişim ülkesi
    /// </summary>
    public string CountryCode { get; set; }

    /// <summary>
    /// Oturum boyunca gerçekleşen işlem sayısı
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Oturum boyunca hızlı sayfa/sekme geçişleri
    /// </summary>
    public int RapidNavigationCount { get; set; }

    /// <summary>
    /// Hesap ayarları değiştirildi mi?
    /// </summary>
    public bool SettingsChanged { get; set; }

    /// <summary>
    /// Oturum ek verileri
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}