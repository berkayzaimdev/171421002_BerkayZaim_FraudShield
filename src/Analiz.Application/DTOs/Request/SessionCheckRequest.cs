using System.ComponentModel.DataAnnotations;

namespace Analiz.Application.DTOs.Request;

/// <summary>
/// Oturum kontrolü isteği
/// </summary>
public class SessionCheckRequest
{
    /// <summary>
    /// Oturum ID'si
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Hesap ID'si
    /// </summary>
    [Required]
    public Guid AccountId { get; set; }

    /// <summary>
    /// Oturum başlangıç zamanı
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Son aktivite zamanı
    /// </summary>
    [Required]
    public DateTime LastActivityTime { get; set; }

    /// <summary>
    /// Oturum süresi (dakika)
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// IP adresi
    /// </summary>
    [Required]
    public string IpAddress { get; set; }

    /// <summary>
    /// Cihaz ID'si
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string UserAgent { get; set; }


    /// <summary>
    /// Hızlı gezinme sayısı
    /// </summary>
    public int RapidNavigationCount { get; set; }


    /// <summary>
    /// Ek veriler
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; }
}