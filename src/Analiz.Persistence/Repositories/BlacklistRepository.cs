using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.Exceptions;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

/// <summary>
/// Kara liste veritabanı işlemleri
/// </summary>
public class BlacklistRepository : IBlacklistRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<BlacklistRepository> _logger;

    public BlacklistRepository(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<BlacklistRepository> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BlacklistItem?> GetByIdAsync(Guid id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.BlacklistItems
                .FirstOrDefaultAsync(bi => bi.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi getirilirken hata oluştu. ID: {Id}", id);
            throw new RepositoryException($"Kara liste öğesi getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<BlacklistItem>> GetAllAsync(int limit = 100, int offset = 0)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.BlacklistItems
                .OrderByDescending(bi => bi.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğeleri getirilirken hata oluştu");
            throw new RepositoryException($"Kara liste öğeleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<BlacklistItem>> GetByTypeAsync(BlacklistType type)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.BlacklistItems
                .Where(bi => bi.Type == type)
                .OrderByDescending(bi => bi.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tip bazında kara liste öğeleri getirilirken hata oluştu. Type: {Type}", type);
            throw new RepositoryException($"Tip bazında kara liste öğeleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<BlacklistItem>> GetActiveByTypeAsync(BlacklistType type)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            return await context.BlacklistItems
                .Where(bi => bi.Type == type && 
                           bi.Status == BlacklistStatus.Active &&
                           (bi.ExpiryDate == null || bi.ExpiryDate > now))
                .OrderByDescending(bi => bi.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif kara liste öğeleri getirilirken hata oluştu. Type: {Type}", type);
            throw new RepositoryException($"Aktif kara liste öğeleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<BlacklistItem?> GetByTypeAndValueAsync(BlacklistType type, string value)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.BlacklistItems
                .FirstOrDefaultAsync(bi => bi.Type == type && bi.Value == value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tip ve değer bazında kara liste öğesi getirilirken hata oluştu. Type: {Type}, Value: {Value}", type, value);
            throw new RepositoryException($"Tip ve değer bazında kara liste öğesi getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<BlacklistItem>> GetExpiredItemsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            return await context.BlacklistItems
                .Where(bi => bi.ExpiryDate != null && bi.ExpiryDate <= now && bi.Status == BlacklistStatus.Active)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Süresi dolmuş kara liste öğeleri getirilirken hata oluştu");
            throw new RepositoryException($"Süresi dolmuş kara liste öğeleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> IsBlacklistedAsync(BlacklistType type, string value)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            return await context.BlacklistItems
                .AnyAsync(bi => bi.Type == type && 
                              bi.Value == value && 
                              bi.Status == BlacklistStatus.Active &&
                              (bi.ExpiryDate == null || bi.ExpiryDate > now));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste kontrolü yapılırken hata oluştu. Type: {Type}, Value: {Value}", type, value);
            throw new RepositoryException($"Kara liste kontrolü yapılırken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> AddAsync(BlacklistItem item)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await context.BlacklistItems.AddAsync(item);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi eklenirken hata oluştu");
            throw new RepositoryException($"Kara liste öğesi eklenirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateAsync(BlacklistItem item)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.BlacklistItems.Update(item);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi güncellenirken hata oluştu");
            throw new RepositoryException($"Kara liste öğesi güncellenirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var item = await context.BlacklistItems.FindAsync(id);
            if (item == null)
                return false;

            context.BlacklistItems.Remove(item);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi silinirken hata oluştu. ID: {Id}", id);
            throw new RepositoryException($"Kara liste öğesi silinirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<int> GetCountByTypeAsync(BlacklistType type)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.BlacklistItems
                .CountAsync(bi => bi.Type == type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tip bazında kara liste sayısı alınırken hata oluştu. Type: {Type}", type);
            throw new RepositoryException($"Tip bazında kara liste sayısı alınırken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<int> GetActiveCountByTypeAsync(BlacklistType type)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            return await context.BlacklistItems
                .CountAsync(bi => bi.Type == type && 
                                bi.Status == BlacklistStatus.Active &&
                                (bi.ExpiryDate == null || bi.ExpiryDate > now));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif kara liste sayısı alınırken hata oluştu. Type: {Type}", type);
            throw new RepositoryException($"Aktif kara liste sayısı alınırken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<int> CleanupExpiredItemsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            var expiredItems = await context.BlacklistItems
                .Where(bi => bi.ExpiryDate != null && bi.ExpiryDate <= now && bi.Status == BlacklistStatus.Active)
                .ToListAsync();

            foreach (var item in expiredItems)
            {
                item.Status = BlacklistStatus.Expired;
            }

            await context.SaveChangesAsync();
            return expiredItems.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Süresi dolmuş kara liste öğeleri temizlenirken hata oluştu");
            throw new RepositoryException($"Süresi dolmuş kara liste öğeleri temizlenirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<string>> GetBlacklistedIpsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            return await context.BlacklistItems
                .Where(bi => bi.Type == BlacklistType.IpAddress && 
                           bi.Status == BlacklistStatus.Active &&
                           (bi.ExpiryDate == null || bi.ExpiryDate > now))
                .Select(bi => bi.Value)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara listedeki IP'ler getirilirken hata oluştu");
            throw new RepositoryException($"Kara listedeki IP'ler getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Guid>> GetBlacklistedAccountsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            var accountValues = await context.BlacklistItems
                .Where(bi => bi.Type == BlacklistType.Account && 
                           bi.Status == BlacklistStatus.Active &&
                           (bi.ExpiryDate == null || bi.ExpiryDate > now))
                .Select(bi => bi.Value)
                .ToListAsync();
            
            var accountIds = new List<Guid>();
            foreach (var value in accountValues)
            {
                if (Guid.TryParse(value, out Guid accountId))
                {
                    accountIds.Add(accountId);
                }
            }
            return accountIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara listedeki hesaplar getirilirken hata oluştu");
            throw new RepositoryException($"Kara listedeki hesaplar getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<string>> GetBlacklistedDevicesAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            return await context.BlacklistItems
                .Where(bi => bi.Type == BlacklistType.Device && 
                           bi.Status == BlacklistStatus.Active &&
                           (bi.ExpiryDate == null || bi.ExpiryDate > now))
                .Select(bi => bi.Value)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara listedeki cihazlar getirilirken hata oluştu");
            throw new RepositoryException($"Kara listedeki cihazlar getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<string>> GetBlacklistedCountriesAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            return await context.BlacklistItems
                .Where(bi => bi.Type == BlacklistType.Country && 
                           bi.Status == BlacklistStatus.Active &&
                           (bi.ExpiryDate == null || bi.ExpiryDate > now))
                .Select(bi => bi.Value)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara listedeki ülkeler getirilirken hata oluştu");
            throw new RepositoryException($"Kara listedeki ülkeler getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> AddBlacklistItemAsync(BlacklistItem item)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await context.BlacklistItems.AddAsync(item);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi eklenirken hata oluştu");
            throw new RepositoryException($"Kara liste öğesi eklenirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> RemoveFromBlacklistAsync(Guid id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var item = await context.BlacklistItems.FindAsync(id);
            if (item == null)
                return false;

            context.BlacklistItems.Remove(item);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi silinirken hata oluştu. ID: {Id}", id);
            throw new RepositoryException($"Kara liste öğesi silinirken hata oluştu: {ex.Message}", ex);
        }
    }
}