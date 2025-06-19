namespace Analiz.Application.Interfaces.Services;

/// <summary>
/// Kara liste kontrolü servisi arayüzü
/// </summary>
public interface IBlacklistService
{
    /// <summary>
    /// IP adresi kara listede mi?
    /// </summary>
    Task<bool> IsIpBlacklistedAsync(string ipAddress);

    /// <summary>
    /// Hesap kara listede mi?
    /// </summary>
    Task<bool> IsAccountBlacklistedAsync(Guid accountId);

    /// <summary>
    /// Cihaz kara listede mi?
    /// </summary>
    Task<bool> IsDeviceBlacklistedAsync(string deviceId);

    /// <summary>
    /// Ülke kara listede mi?
    /// </summary>
    Task<bool> IsCountryBlacklistedAsync(string countryCode);

    /// <summary>
    /// IP adresini kara listeye ekle
    /// </summary>
    Task<bool> AddIpToBlacklistAsync(string ipAddress, string reason, TimeSpan? duration = null);

    /// <summary>
    /// Hesabı kara listeye ekle
    /// </summary>
    Task<bool> AddAccountToBlacklistAsync(Guid accountId, string reason, TimeSpan? duration = null);

    /// <summary>
    /// Cihazı kara listeye ekle
    /// </summary>
    Task<bool> AddDeviceToBlacklistAsync(string deviceId, string reason, TimeSpan? duration = null);
}