using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class RiskFactorRepository : IRiskFactorRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<RiskFactorRepository> _logger;

    public RiskFactorRepository(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<RiskFactorRepository> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RiskFactor> GetByIdAsync(Guid id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.RiskFactors
                .FirstOrDefaultAsync(rf => rf.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktörü getirilirken hata oluştu. ID: {Id}", id);
            throw new RepositoryException($"Risk faktörü getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<RiskFactor>> GetAllAsync(int limit = 100, int offset = 0)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.RiskFactors
                .OrderByDescending(rf => rf.DetectedAt)
                .ThenByDescending(rf => rf.Severity)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tüm risk faktörleri getirilirken hata oluştu. Limit: {Limit}, Offset: {Offset}", limit, offset);
            throw new RepositoryException($"Tüm risk faktörleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<RiskFactor>> GetByTransactionIdAsync(string transactionId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // String transaction ID'yi Guid'e çevir
            if (!Guid.TryParse(transactionId, out var guidTransactionId))
            {
                return new List<RiskFactor>();
            }
            
            return await context.RiskFactors
                .Where(rf => rf.TransactionId == guidTransactionId)
                .OrderByDescending(rf => rf.Severity)
                .ThenByDescending(rf => rf.Confidence)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem ID'ye göre risk faktörleri getirilirken hata oluştu. TransactionId: {TransactionId}", transactionId);
            throw new RepositoryException($"İşlem ID'ye göre risk faktörleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<RiskFactor>> GetAllForTransactionAsync(Guid transactionId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.RiskFactors
                .Where(rf => rf.TransactionId == transactionId)
                .OrderByDescending(rf => rf.Severity)
                .ThenByDescending(rf => rf.Confidence)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem için risk faktörleri getirilirken hata oluştu. TransactionId: {TransactionId}",
                transactionId);
            throw new RepositoryException($"İşlem için risk faktörleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<RiskFactor>> GetByTypeAsync(RiskFactorType type)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.RiskFactors
                .Where(rf => rf.Code == type.ToString())
                .OrderByDescending(rf => rf.Confidence)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Belirli tipteki risk faktörleri getirilirken hata oluştu. Type: {Type}", type);
            throw new RepositoryException($"Belirli tipteki risk faktörleri getirilirken hata oluştu: {ex.Message}",
                ex);
        }
    }

    public async Task<List<RiskFactor>> GetByUserIdAsync(string userId, DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Kullanıcının işlemlerini al
            var query = context.Transactions
                .Where(t => t.UserId == userId && !t.IsDeleted);

            // Tarih filtreleri uygula
            if (startDate.HasValue)
                query = query.Where(t => t.TransactionTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.TransactionTime <= endDate.Value);

            // İşlem ID'lerini al
            var transactionIds = await query
                .Select(t => t.Id)
                .ToListAsync();

            // Bu işlemlere ait risk faktörlerini getir
            return await context.RiskFactors
                .Where(rf => transactionIds.Contains(rf.TransactionId))
                .OrderByDescending(rf => rf.Severity)
                .ThenByDescending(rf => rf.Confidence)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcının risk faktörleri getirilirken hata oluştu. UserId: {UserId}", userId);
            throw new RepositoryException($"Kullanıcının risk faktörleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<RiskFactor>> GetHighSeverityFactorsAsync(RiskLevel minSeverity, int limit = 100)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.RiskFactors
                .Where(rf => rf.Severity >= minSeverity)
                .OrderByDescending(rf => rf.Severity)
                .ThenByDescending(rf => rf.Confidence)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yüksek şiddetli risk faktörleri getirilirken hata oluştu. MinSeverity: {MinSeverity}",
                minSeverity);
            throw new RepositoryException($"Yüksek şiddetli risk faktörleri getirilirken hata oluştu: {ex.Message}",
                ex);
        }
    }

    public async Task<Dictionary<string, int>> GetMostCommonFactorsAsync(string userId, int limit = 10)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Kullanıcının işlemlerini al
            var transactionIds = await context.Transactions
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .Select(t => t.Id)
                .ToListAsync();

            // Bu işlemlere ait risk faktörlerini grupla ve say
            var factorCounts = await context.RiskFactors
                .Where(rf => transactionIds.Contains(rf.TransactionId))
                .GroupBy(rf => rf.Code)
                .Select(g => new { FactorCode = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(limit)
                .ToListAsync();

            // Dictionary'ye dönüştür
            return factorCounts.ToDictionary(x => x.FactorCode, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "En yaygın risk faktörleri getirilirken hata oluştu. UserId: {UserId}", userId);
            throw new RepositoryException($"En yaygın risk faktörleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<List<RiskFactor>> GetByAnalysisResultIdAsync(Guid analysisResultId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.RiskFactors
                .Where(rf => rf.AnalysisResultId == analysisResultId)
                .OrderByDescending(rf => rf.Severity)
                .ThenByDescending(rf => rf.Confidence)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz sonucu için risk faktörleri getirilirken hata oluştu. AnalysisResultId: {AnalysisResultId}", analysisResultId);
            throw new RepositoryException($"Analiz sonucu için risk faktörleri getirilirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> AddAsync(RiskFactor riskFactor)
    {
        try
        {
            // ID atanmamışsa otomatik ata
            if (riskFactor.Id == Guid.Empty) riskFactor.Id = Guid.NewGuid();

            await using var context = await _contextFactory.CreateDbContextAsync();
            await context.RiskFactors.AddAsync(riskFactor);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktörü eklenirken hata oluştu");
            throw new RepositoryException($"Risk faktörü eklenirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> AddRangeAsync(List<RiskFactor> riskFactors)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await context.RiskFactors.AddRangeAsync(riskFactors);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktörleri toplu eklenirken hata oluştu");
            throw new RepositoryException($"Risk faktörleri toplu eklenirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateAsync(RiskFactor riskFactor)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.RiskFactors.Update(riskFactor);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktörü güncellenirken hata oluştu");
            throw new RepositoryException($"Risk faktörü güncellenirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var riskFactor = await context.RiskFactors.FindAsync(id);
            if (riskFactor == null)
                return false;

            context.RiskFactors.Remove(riskFactor);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktörü silinirken hata oluştu. ID: {Id}", id);
            throw new RepositoryException($"Risk faktörü silinirken hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<int> GetFactorCountByTypeAsync(string userId, RiskFactorType type)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Kullanıcının işlemlerini al
            var transactionIds = await context.Transactions
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .Select(t => t.Id)
                .ToListAsync();

            // Belirli tipteki risk faktörlerini say
            return await context.RiskFactors
                .Where(rf => transactionIds.Contains(rf.TransactionId) && rf.Code == type.ToString())
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktör sayısı alınırken hata oluştu. UserId: {UserId}, Type: {Type}", userId,
                type);
            throw new RepositoryException($"Risk faktör sayısı alınırken hata oluştu: {ex.Message}", ex);
        }
    }
}