using System.Text.Json;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

/// <summary>
/// Fraud kural repository implementasyonu
/// </summary>
public class FraudRuleRepository : IFraudRuleRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<FraudRuleRepository> _logger;

    // Cache anahtarları
    private const string ALL_RULES_CACHE_KEY = "FraudRules_All";
    private const string ACTIVE_RULES_CACHE_KEY = "FraudRules_Active";
    private const string RULE_BY_ID_CACHE_KEY_PREFIX = "FraudRule_Id_";
    private const string RULE_BY_CODE_CACHE_KEY_PREFIX = "FraudRule_Code_";
    private const string RULES_BY_CATEGORY_CACHE_KEY_PREFIX = "FraudRules_Category_";

    // Cache yapılandırması
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };

    public FraudRuleRepository(
        ApplicationDbContext dbContext,
        IDistributedCache cache,
        ILogger<FraudRuleRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm kuralları getir
    /// </summary>
    public async Task<IEnumerable<FraudRule>> GetAllRulesAsync()
    {
        try
        {
            // Cache'den kuralları getirmeyi dene
            var cachedRulesJson = await _cache.GetStringAsync(ALL_RULES_CACHE_KEY);

            if (!string.IsNullOrEmpty(cachedRulesJson))
            {
                _logger.LogDebug("Cache hit for all fraud rules");
                return JsonSerializer.Deserialize<List<FraudRule>>(cachedRulesJson);
            }

            // Cache'de yoksa veritabanından getir
            _logger.LogDebug("Cache miss for all fraud rules, fetching from database");
            var rules = await _dbContext.FraudRules
                .OrderBy(r => r.Priority)
                .ToListAsync();

            // Cache'e kaydet
            await _cache.SetStringAsync(
                ALL_RULES_CACHE_KEY,
                JsonSerializer.Serialize(rules),
                _cacheOptions);

            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all fraud rules");
            throw;
        }
    }

    /// <summary>
    /// Aktif kuralları getir
    /// </summary>
    public async Task<IEnumerable<FraudRule>> GetActiveRulesAsync()
    {
        try
        {
            // Cache'den aktif kuralları getirmeyi dene
            var cachedRulesJson = await _cache.GetStringAsync(ACTIVE_RULES_CACHE_KEY);

            if (!string.IsNullOrEmpty(cachedRulesJson))
            {
                _logger.LogDebug("Cache hit for active fraud rules");
                return JsonSerializer.Deserialize<List<FraudRule>>(cachedRulesJson);
            }

            // Cache'de yoksa veritabanından getir
            _logger.LogDebug("Cache miss for active fraud rules, fetching from database");
            var now = DateTime.UtcNow;
            var rules = await _dbContext.FraudRules
                .Where(r => r.Status == RuleStatus.Active || r.Status == RuleStatus.TestMode)
                .Where(r => r.ValidFrom == null || r.ValidFrom <= now)
                .Where(r => r.ValidTo == null || r.ValidTo >= now)
                .OrderBy(r => r.Priority)
                .ToListAsync();

            // Cache'e kaydet
            await _cache.SetStringAsync(
                ACTIVE_RULES_CACHE_KEY,
                JsonSerializer.Serialize(rules),
                _cacheOptions);

            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active fraud rules");
            throw;
        }
    }

    /// <summary>
    /// Belirli bir kategorideki kuralları getir
    /// </summary>
    public async Task<IEnumerable<FraudRule>> GetRulesByCategoryAsync(RuleCategory category)
    {
        try
        {
            // Cache anahtarı
            var cacheKey = $"{RULES_BY_CATEGORY_CACHE_KEY_PREFIX}{category}";

            // Cache'den kategori kurallarını getirmeyi dene
            var cachedRulesJson = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedRulesJson))
            {
                _logger.LogDebug("Cache hit for fraud rules in category {Category}", category);
                return JsonSerializer.Deserialize<List<FraudRule>>(cachedRulesJson);
            }

            // Cache'de yoksa veritabanından getir
            _logger.LogDebug("Cache miss for fraud rules in category {Category}, fetching from database", category);
            var rules = await _dbContext.FraudRules
                .Where(r => r.Category == category)
                .OrderBy(r => r.Priority)
                .ToListAsync();

            // Cache'e kaydet
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(rules),
                _cacheOptions);

            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud rules for category {Category}", category);
            throw;
        }
    }

    /// <summary>
    /// ID'ye göre kural getir
    /// </summary>
    public async Task<FraudRule> GetRuleByIdAsync(Guid id)
    {
        try
        {
            // Cache anahtarı
            var cacheKey = $"{RULE_BY_ID_CACHE_KEY_PREFIX}{id}";

            // Cache'den kuralı getirmeyi dene
            var cachedRuleJson = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedRuleJson))
            {
                _logger.LogDebug("Cache hit for fraud rule with ID {RuleId}", id);
                return JsonSerializer.Deserialize<FraudRule>(cachedRuleJson);
            }

            // Cache'de yoksa veritabanından getir
            _logger.LogDebug("Cache miss for fraud rule with ID {RuleId}, fetching from database", id);
            var rule = await _dbContext.FraudRules
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule != null)
                // Cache'e kaydet
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(rule),
                    _cacheOptions);

            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud rule with ID {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Kural koduna göre kural getir
    /// </summary>
    public async Task<FraudRule> GetRuleByCodeAsync(string ruleCode)
    {
        try
        {
            // Cache anahtarı
            var cacheKey = $"{RULE_BY_CODE_CACHE_KEY_PREFIX}{ruleCode}";

            // Cache'den kuralı getirmeyi dene
            var cachedRuleJson = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedRuleJson))
            {
                _logger.LogDebug("Cache hit for fraud rule with code {RuleCode}", ruleCode);
                return JsonSerializer.Deserialize<FraudRule>(cachedRuleJson);
            }

            // Cache'de yoksa veritabanından getir
            _logger.LogDebug("Cache miss for fraud rule with code {RuleCode}, fetching from database", ruleCode);
            var rule = await _dbContext.FraudRules
                .FirstOrDefaultAsync(r => r.RuleCode == ruleCode);

            if (rule != null)
                // Cache'e kaydet
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(rule),
                    _cacheOptions);

            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud rule with code {RuleCode}", ruleCode);
            throw;
        }
    }

    /// <summary>
    /// Kural ekle
    /// </summary>
    public async Task<FraudRule> AddRuleAsync(FraudRule rule)
    {
        try
        {
            await _dbContext.FraudRules.AddAsync(rule);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Added new fraud rule: {RuleCode} - {RuleName}", rule.RuleCode, rule.Name);

            // Cache'i temizle
            await InvalidateCacheAsync();

            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding fraud rule: {RuleCode} - {RuleName}", rule.RuleCode, rule.Name);
            throw;
        }
    }

    /// <summary>
    /// Kural güncelle
    /// </summary>
    public async Task<FraudRule> UpdateRuleAsync(FraudRule rule)
    {
        try
        {
            _dbContext.FraudRules.Update(rule);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated fraud rule: {RuleCode} - {RuleName}", rule.RuleCode, rule.Name);

            // Cache'i temizle
            await InvalidateCacheAsync();

            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud rule: {RuleCode} - {RuleName}", rule.RuleCode, rule.Name);
            throw;
        }
    }

    /// <summary>
    /// Kural sil (soft delete)
    /// </summary>
    public async Task<bool> DeleteRuleAsync(Guid id)
    {
        try
        {
            var rule = await _dbContext.FraudRules.FindAsync(id);

            if (rule == null) return false;

            // Soft delete (burada gerçekten silmek yerine arşivliyoruz)
            rule.Status = RuleStatus.Archived;
            rule.LastModified = DateTime.UtcNow;
            rule.ModifiedBy = "System (Delete)";

            _dbContext.FraudRules.Update(rule);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted (archived) fraud rule: {RuleCode} - {RuleName}", rule.RuleCode, rule.Name);

            // Cache'i temizle
            await InvalidateCacheAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fraud rule with ID {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Cache'i yenile
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing fraud rule cache");

            // Cache'i temizle
            await InvalidateCacheAsync();

            // Tüm kuralları yeniden yükle
            var allRules = await _dbContext.FraudRules
                .OrderBy(r => r.Priority)
                .ToListAsync();

            // Aktif kuralları filtrele
            var now = DateTime.UtcNow;
            var activeRules = allRules
                .Where(r => r.Status == RuleStatus.Active || r.Status == RuleStatus.TestMode)
                .Where(r => r.ValidFrom == null || r.ValidFrom <= now)
                .Where(r => r.ValidTo == null || r.ValidTo >= now)
                .ToList();

            // Tüm kuralları cache'e kaydet
            await _cache.SetStringAsync(
                ALL_RULES_CACHE_KEY,
                JsonSerializer.Serialize(allRules),
                _cacheOptions);

            // Aktif kuralları cache'e kaydet
            await _cache.SetStringAsync(
                ACTIVE_RULES_CACHE_KEY,
                JsonSerializer.Serialize(activeRules),
                _cacheOptions);

            // Kategoriye göre grupla ve cache'e kaydet
            var rulesByCategory = allRules.GroupBy(r => r.Category);
            foreach (var group in rulesByCategory)
            {
                var cacheKey = $"{RULES_BY_CATEGORY_CACHE_KEY_PREFIX}{group.Key}";
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(group.ToList()),
                    _cacheOptions);
            }

            // Her bir kural için detayları cache'e kaydet
            foreach (var rule in allRules)
            {
                // ID'ye göre
                var idCacheKey = $"{RULE_BY_ID_CACHE_KEY_PREFIX}{rule.Id}";
                await _cache.SetStringAsync(
                    idCacheKey,
                    JsonSerializer.Serialize(rule),
                    _cacheOptions);

                // Koda göre
                var codeCacheKey = $"{RULE_BY_CODE_CACHE_KEY_PREFIX}{rule.RuleCode}";
                await _cache.SetStringAsync(
                    codeCacheKey,
                    JsonSerializer.Serialize(rule),
                    _cacheOptions);
            }

            _logger.LogInformation("Refreshed fraud rule cache with {RuleCount} rules", allRules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing fraud rule cache");
            throw;
        }
    }

    /// <summary>
    /// Cache'i temizle
    /// </summary>
    private async Task InvalidateCacheAsync()
    {
        try
        {
            await _cache.RemoveAsync(ALL_RULES_CACHE_KEY);
            await _cache.RemoveAsync(ACTIVE_RULES_CACHE_KEY);

            // Diğer cache anahtarlarını silmek için daha karmaşık bir yaklaşım gerekiyor
            // Gerçek uygulamada Redis SCAN veya benzeri bir yaklaşım kullanılabilir
            // Burada sadece temel cache anahtarlarını temizliyoruz

            _logger.LogDebug("Invalidated fraud rule cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating fraud rule cache");
            throw;
        }
    }
}