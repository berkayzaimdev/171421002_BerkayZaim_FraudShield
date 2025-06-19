using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Analiz.Persistence.Repositories;

public class FraudAlertRepository : IFraudAlertRepository
{
    private readonly ApplicationDbContext _dbContext;

    public FraudAlertRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<FraudAlert>> GetAllAsync()
    {
        return await _dbContext.FraudAlerts
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FraudAlert>> GetActiveAlertsAsync()
    {
        return await _dbContext.FraudAlerts
            .Where(a => a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FraudAlert>> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.FraudAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FraudAlert>> GetByTransactionIdAsync(Guid transactionId)
    {
        return await _dbContext.FraudAlerts
            .Where(a => a.TransactionId == transactionId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FraudAlert>> GetByStatusAsync(AlertStatus status)
    {
        return await _dbContext.FraudAlerts
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<FraudAlert> GetByIdAsync(Guid id)
    {
        return await _dbContext.FraudAlerts
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<FraudAlert> AddAsync(FraudAlert alert)
    {
        await _dbContext.FraudAlerts.AddAsync(alert);
        await _dbContext.SaveChangesAsync();
        return alert;
    }

    public async Task UpdateAsync(FraudAlert alert)
    {
        _dbContext.FraudAlerts.Update(alert);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var alert = await _dbContext.FraudAlerts.FindAsync(id);
        if (alert != null)
        {
            _dbContext.FraudAlerts.Remove(alert);
            await _dbContext.SaveChangesAsync();
        }
    }
}