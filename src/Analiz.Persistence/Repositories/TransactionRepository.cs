using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<TransactionRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<Transaction> GetTransactionAsync(Guid transactionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    public async Task<List<Transaction>> GetUserTransactionHistoryAsync(string userId, int limit)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transactions
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.TransactionTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetUserTransactionsAsync(string userId, TimeSpan period)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var startDate = DateTime.Now.Subtract(period);

        return await context.Transactions
            .Where(t => t.UserId == userId &&
                        t.TransactionTime >= startDate &&
                        !t.IsDeleted)
            .OrderByDescending(t => t.TransactionTime)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsBetweenDatesAsync(
        DateTime startDate,
        DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transactions
            .Where(t => t.TransactionTime >= startDate && t.TransactionTime <= endDate)
            .OrderByDescending(t => t.TransactionTime)
            .ToListAsync();
    }

    public async Task<Transaction> SaveTransactionAsync(Transaction transaction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }
    

    public async Task<int> GetTransactionCountAsync(string userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Transactions
                .Where(t => t.UserId == userId &&
                            t.TransactionTime >= startDate &&
                            t.TransactionTime <= endDate &&
                            !t.IsDeleted)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting transactions for user {UserId} between {StartDate} and {EndDate}",
                userId, startDate, endDate);
            return 0; // Return 0 in case of error
        }
    }

    public async Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var result = await context.AnalysisResults
                .Include(a => a.RiskFactors)
                .FirstOrDefaultAsync(a => a.Id == analysisId && !a.IsDeleted);

            if (result == null)
            {
                _logger.LogWarning("Analysis result not found for ID: {AnalysisId}", analysisId);
                throw new Exception($"Analysis result not found with ID: {analysisId}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis result {AnalysisId}", analysisId);
            throw new RepositoryException($"Error retrieving analysis result: {analysisId}", ex);
        }
    }

    public async Task<List<FraudAlert>> GetActiveAlertsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.FraudAlerts
                .Include(a => a.Factors)
                .Where(a => a.Status == AlertStatus.Active && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active fraud alerts");
            throw new RepositoryException("Error retrieving active fraud alerts", ex);
        }
    }

    public async Task UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Transactions.FindAsync(transactionId);
        if (transaction != null)
        {
            transaction.UpdateStatus(status);
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid transactionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transactions.AnyAsync(t => t.Id == transactionId);
    }
}