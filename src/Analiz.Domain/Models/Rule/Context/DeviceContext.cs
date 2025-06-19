namespace Analiz.Domain.Entities.Rule.Context;

/// <summary>
/// Cihaz bağlamı
/// </summary>
public class DeviceContext
{
    /// <summary>
    /// Cihaz ID'si
    /// </summary>
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
    public string IpAddress { get; set; }

    /// <summary>
    /// İlk kullanım tarihi
    /// </summary>
    public DateTime? FirstSeenDate { get; set; }

    /// <summary>
    /// Son kullanım tarihi
    /// </summary>
    public DateTime? LastSeenDate { get; set; }

    /// <summary>
    /// Hesaba kayıtlı mı?
    /// </summary>
    public bool IsRegistered { get; set; }

    /// <summary>
    /// Güvenilir mi?
    /// </summary>
    public bool IsTrusted { get; set; }

    /// <summary>
    /// Jailbreak/root yapılmış mı?
    /// </summary>
    public bool IsJailbroken { get; set; }

    /// <summary>
    /// Emülatör mü?
    /// </summary>
    public bool IsEmulator { get; set; }

    /// <summary>
    /// Cihazın kullanıldığı hesap sayısı
    /// </summary>
    public int LinkedAccountCount { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı hesap sayısı
    /// </summary>
    public int UniqueAccountCount24h { get; set; }

    /// <summary>
    /// Son 24 saatteki farklı IP sayısı
    /// </summary>
    public int UniqueIpCount24h { get; set; }

    /// <summary>
    /// GPS lokasyonu
    /// </summary>
    public GpsLocation GpsLocation { get; set; }

    /// <summary>
    /// Cihaz ek verileri
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}