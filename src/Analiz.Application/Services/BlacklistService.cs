using System.Text.Json;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

/// <summary>
/// Kara liste kontrolü servisi
/// </summary>
public class BlacklistService : IBlacklistService
{
    private readonly IBlacklistRepository _blacklistRepository;
    private readonly IDistributedCache _redisCache;
    private readonly ILogger<BlacklistService> _logger;

    // Cache süreleri ve ayarları
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);
    private readonly DistributedCacheEntryOptions _cacheOptions;

    // Cache anahtarları
    private const string IP_BLACKLIST_CACHE_KEY = "blacklist:ips";
    private const string ACCOUNT_BLACKLIST_CACHE_KEY = "blacklist:accounts";
    private const string DEVICE_BLACKLIST_CACHE_KEY = "blacklist:devices";
    private const string COUNTRY_BLACKLIST_CACHE_KEY = "blacklist:countries";

    public BlacklistService(
        IBlacklistRepository blacklistRepository,
        IDistributedCache redisCache,
        ILogger<BlacklistService> logger)
    {
        _blacklistRepository = blacklistRepository ?? throw new ArgumentNullException(nameof(blacklistRepository));
        _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Cache ayarlarını oluştur
        _cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheDuration,
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
    }

    /// <summary>
    /// IP adresi kara listede mi?
    /// </summary>
    public async Task<bool> IsIpBlacklistedAsync(string ipAddress)
    {
        try
        {
            return await _blacklistRepository.IsBlacklistedAsync(BlacklistType.IpAddress, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP adresi kara liste kontrolü yapılırken hata oluştu. IP: {IP}", ipAddress);
            throw;
        }
    }

    /// <summary>
    /// Hesap kara listede mi?
    /// </summary>
    public async Task<bool> IsAccountBlacklistedAsync(Guid accountId)
    {
        try
        {
            return await _blacklistRepository.IsBlacklistedAsync(BlacklistType.Account, accountId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hesap kara liste kontrolü yapılırken hata oluştu. AccountId: {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Cihaz kara listede mi?
    /// </summary>
    public async Task<bool> IsDeviceBlacklistedAsync(string deviceId)
    {
        try
        {
            return await _blacklistRepository.IsBlacklistedAsync(BlacklistType.Device, deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cihaz kara liste kontrolü yapılırken hata oluştu. DeviceId: {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Ülke kara listede mi?
    /// </summary>
    public async Task<bool> IsCountryBlacklistedAsync(string countryCode)
    {
        try
        {
            return await _blacklistRepository.IsBlacklistedAsync(BlacklistType.Country, countryCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ülke kara liste kontrolü yapılırken hata oluştu. CountryCode: {CountryCode}", countryCode);
            throw;
        }
    }

    /// <summary>
    /// IP adresini kara listeye ekle
    /// </summary>
    public async Task<bool> AddIpToBlacklistAsync(string ipAddress, string reason, TimeSpan? duration = null)
    {
        try
        {
            var item = BlacklistItem.Create(
                BlacklistType.IpAddress,
                ipAddress,
                reason,
                duration: duration,
                addedBy: "system");

            return await _blacklistRepository.AddAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP adresi kara listeye eklenirken hata oluştu. IP: {IP}", ipAddress);
            throw;
        }
    }

    /// <summary>
    /// Hesabı kara listeye ekle
    /// </summary>
    public async Task<bool> AddAccountToBlacklistAsync(Guid accountId, string reason, TimeSpan? duration = null)
    {
        try
        {
            var item = BlacklistItem.Create(
                BlacklistType.Account,
                accountId.ToString(),
                reason,
                duration: duration,
                addedBy: "system");

            return await _blacklistRepository.AddAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hesap kara listeye eklenirken hata oluştu. AccountId: {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Cihazı kara listeye ekle
    /// </summary>
    public async Task<bool> AddDeviceToBlacklistAsync(string deviceId, string reason, TimeSpan? duration = null)
    {
        try
        {
            var item = BlacklistItem.Create(
                BlacklistType.Device,
                deviceId,
                reason,
                duration: duration,
                addedBy: "system");

            return await _blacklistRepository.AddAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cihaz kara listeye eklenirken hata oluştu. DeviceId: {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Ülkeyi kara listeye ekle
    /// </summary>
    public async Task<bool> AddCountryToBlacklistAsync(string countryCode, string reason, TimeSpan? duration = null)
    {
        if (string.IsNullOrEmpty(countryCode))
            return false;

        try
        {
            // Ülkeyi kara listeye ekle
            var blacklistItem = BlacklistItem.Create(
                BlacklistType.Country,
                countryCode,
                reason,
                duration: duration);

            var result = await _blacklistRepository.AddBlacklistItemAsync(blacklistItem);

            // Cache'i temizle
            await _redisCache.RemoveAsync(COUNTRY_BLACKLIST_CACHE_KEY);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding country {CountryCode} to blacklist", countryCode);
            return false;
        }
    }

    /// <summary>
    /// Tüm kara liste cache'lerini yenile
    /// </summary>
    public async Task RefreshAllCachesAsync()
    {
        try
        {
            // Tüm cache'leri temizle
            await _redisCache.RemoveAsync(IP_BLACKLIST_CACHE_KEY);
            await _redisCache.RemoveAsync(ACCOUNT_BLACKLIST_CACHE_KEY);
            await _redisCache.RemoveAsync(DEVICE_BLACKLIST_CACHE_KEY);
            await _redisCache.RemoveAsync(COUNTRY_BLACKLIST_CACHE_KEY);

            // Veritabanından yükle ve cache'e ekle
            await IsIpBlacklistedAsync("127.0.0.1"); // Yükleme için tetikleyici
            await IsAccountBlacklistedAsync(Guid.Empty); // Yükleme için tetikleyici
            await IsDeviceBlacklistedAsync("device-id"); // Yükleme için tetikleyici
            await IsCountryBlacklistedAsync("XX"); // Yükleme için tetikleyici

            _logger.LogInformation("All blacklist caches refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing blacklist caches");
        }
    }
}