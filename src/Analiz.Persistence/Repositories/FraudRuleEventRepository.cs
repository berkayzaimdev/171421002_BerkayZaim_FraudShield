using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

/// <summary>
/// Fraud olay repository implementasyonu
/// </summary>
public class FraudRuleEventRepository : IFraudRuleEventRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<FraudRuleEventRepository> _logger;

    public FraudRuleEventRepository(
        ApplicationDbContext dbContext,
        ILogger<FraudRuleEventRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetAllEventsAsync()
    {
        try
        {
            return await _dbContext.FraudRuleEvents
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all fraud events");
            throw;
        }
    }

    /// <summary>
    /// Çözülmemiş olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetUnresolvedEventsAsync()
    {
        try
        {
            return await _dbContext.FraudRuleEvents
                .Where(e => e.ResolvedDate == null)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unresolved fraud events");
            throw;
        }
    }

    /// <summary>
    /// Hesap ID'sine göre olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetEventsByAccountIdAsync(Guid accountId)
    {
        try
        {
            return await _dbContext.FraudRuleEvents
                .Where(e => e.AccountId == accountId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud events for account {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// IP adresine göre olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetEventsByIpAddressAsync(string ipAddress)
    {
        try
        {
            return await _dbContext.FraudRuleEvents
                .Where(e => e.IpAddress == ipAddress)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud events for IP {IpAddress}", ipAddress);
            throw;
        }
    }

    /// <summary>
    /// Kural ID'sine göre olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetEventsByRuleIdAsync(Guid ruleId)
    {
        try
        {
            return await _dbContext.FraudRuleEvents
                .Where(e => e.RuleId == ruleId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud events for rule {RuleId}", ruleId);
            throw;
        }
    }

    /// <summary>
    /// ID'ye göre olay getir
    /// </summary>
    public async Task<FraudRuleEvent> GetEventByIdAsync(Guid id)
    {
        try
        {
            return await _dbContext.FraudRuleEvents
                .FirstOrDefaultAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud event with ID {EventId}", id);
            throw;
        }
    }

    /// <summary>
    /// Olay ekle
    /// </summary>
    public async Task<FraudRuleEvent> AddEventAsync(FraudRuleEvent fraudEvent)
    {
        try
        {
            await _dbContext.FraudRuleEvents.AddAsync(fraudEvent);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Added new fraud event: {EventId} for rule {RuleCode}",
                fraudEvent.Id, fraudEvent.RuleCode);

            return fraudEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding fraud event for rule {RuleCode}", fraudEvent.RuleCode);
            throw;
        }
    }

    /// <summary>
    /// Olay güncelle
    /// </summary>
    public async Task<FraudRuleEvent> UpdateEventAsync(FraudRuleEvent fraudEvent)
    {
        try
        {
            _dbContext.FraudRuleEvents.Update(fraudEvent);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated fraud event: {EventId}", fraudEvent.Id);

            return fraudEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud event {EventId}", fraudEvent.Id);
            throw;
        }
    }

    // Generic metotlar (service layer tarafından kullanılıyor)
    public async Task<IEnumerable<FraudRuleEvent>> GetAllAsync()
    {
        return await GetAllEventsAsync();
    }

    public async Task<FraudRuleEvent> GetByIdAsync(Guid id)
    {
        return await GetEventByIdAsync(id);
    }

    public async Task<IEnumerable<FraudRuleEvent>> GetByAccountIdAsync(Guid accountId)
    {
        return await GetEventsByAccountIdAsync(accountId);
    }

    public async Task<IEnumerable<FraudRuleEvent>> GetByIpAddressAsync(string ipAddress)
    {
        return await GetEventsByIpAddressAsync(ipAddress);
    }

    public async Task UpdateAsync(FraudRuleEvent fraudEvent)
    {
        await UpdateEventAsync(fraudEvent);
    }
}