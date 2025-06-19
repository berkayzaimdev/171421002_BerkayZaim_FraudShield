using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Interfaces.Repositories;

public interface IBlacklistRepository
{
    // Kara listedeki IP'leri çekme
    Task<IEnumerable<string>> GetBlacklistedIpsAsync();

    // Kara listedeki hesapları çekme
    Task<IEnumerable<Guid>> GetBlacklistedAccountsAsync();

    // Kara listedeki cihazları çekme
    Task<IEnumerable<string>> GetBlacklistedDevicesAsync();

    // Kara listedeki ülkeleri çekme
    Task<IEnumerable<string>> GetBlacklistedCountriesAsync();

    // Kara listeye öğe ekleme
    Task<bool> AddBlacklistItemAsync(BlacklistItem item);

    // Kara listeden öğe çıkarma
    Task<bool> RemoveFromBlacklistAsync(Guid id);

    // Süresi dolmuş öğeleri temizleme
    Task<int> CleanupExpiredItemsAsync();

    Task<BlacklistItem?> GetByIdAsync(Guid id);
    Task<List<BlacklistItem>> GetAllAsync(int limit = 100, int offset = 0);
    Task<List<BlacklistItem>> GetByTypeAsync(BlacklistType type);
    Task<List<BlacklistItem>> GetActiveByTypeAsync(BlacklistType type);
    Task<BlacklistItem?> GetByTypeAndValueAsync(BlacklistType type, string value);
    Task<List<BlacklistItem>> GetExpiredItemsAsync();
    Task<bool> IsBlacklistedAsync(BlacklistType type, string value);
    Task<bool> AddAsync(BlacklistItem item);
    Task<bool> UpdateAsync(BlacklistItem item);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetCountByTypeAsync(BlacklistType type);
    Task<int> GetActiveCountByTypeAsync(BlacklistType type);
}