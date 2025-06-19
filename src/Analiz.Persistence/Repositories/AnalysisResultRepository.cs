using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class AnalysisResultRepository : IAnalysisResultRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<AnalysisResultRepository> _logger;

    public AnalysisResultRepository(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<AnalysisResultRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AnalysisResults
            .Include(a => a.RiskFactors)
            .FirstOrDefaultAsync(a => a.Id == analysisId);
    }

    public async Task<AnalysisResult?> GetByTransactionIdAsync(Guid transactionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AnalysisResults
            .Include(a => a.RiskFactors)
            .FirstOrDefaultAsync(a => a.TransactionId == transactionId);
    }

    public async Task<List<AnalysisResult>> GetByTransactionIdsAsync(List<Guid> transactionIds)
    {
        if (!transactionIds.Any()) return new List<AnalysisResult>();
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AnalysisResults
            .Include(a => a.RiskFactors)
            .Where(a => transactionIds.Contains(a.TransactionId))
            .ToListAsync();
    }

    public async Task<List<AnalysisResult>> GetResultsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AnalysisResults
            .Include(r => r.RiskFactors)
            .Where(r => r.AnalyzedAt >= startDate && r.AnalyzedAt <= endDate)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync();
    }

    public async Task<List<AnalysisResult>> GetHighRiskResultsAsync(TimeSpan period)
    {
        var startDate = DateTime.UtcNow.Subtract(period);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AnalysisResults
            .Include(r => r.RiskFactors)
            .Where(r => r.RiskScore.Level >= RiskLevel.High && r.AnalyzedAt >= startDate)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync();
    }

    public async Task<AnalysisResult> SaveResultAsync(AnalysisResult result)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.AnalysisResults.AddAsync(result);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<AnalysisResult> UpdateAsync(AnalysisResult analysisResult)
    {
        try
        {
            _logger.LogInformation("Analiz sonucunu güncelleme: {Id}", analysisResult.Id);

            await using var context = await _contextFactory.CreateDbContextAsync();

            // Entity'nin güncel durumda olduğundan emin olalım
            context.Entry(analysisResult).State = EntityState.Modified;

            // FraudAlert özelliği varsa ve veritabanında yoksa ilişkilendirme yapalım
            if (analysisResult.FraudAlert != null)
            {
                var existingFraudAlert = await context.FraudAlerts
                    .FirstOrDefaultAsync(fa => fa.Id == analysisResult.FraudAlert.Id);

                if (existingFraudAlert == null)
                {
                    // FraudAlert veritabanında yoksa ekleyelim
                    _logger.LogInformation("FraudAlert veritabanına ekleniyor: {Id}", analysisResult.FraudAlert.Id);
                    context.FraudAlerts.Add(analysisResult.FraudAlert);
                }
                else
                {
                    // FraudAlert veritabanında varsa ilişkiyi güncelleyelim
                    _logger.LogInformation("FraudAlert ilişkisi güncelleniyor");
                    existingFraudAlert.SetAnalysisResult(analysisResult);
                    context.FraudAlerts.Update(existingFraudAlert);
                }
            }

            // RiskFactors koleksiyonundaki tüm faktörlerin AnalysisResultId'sini ayarlayalım
            foreach (var riskFactor in analysisResult.RiskFactors)
            {
                riskFactor.AnalysisResultId = analysisResult.Id;
            }

            // Değişiklikleri kaydet
            await context.SaveChangesAsync();

            _logger.LogInformation("Analiz sonucu başarıyla güncellendi: {Id}", analysisResult.Id);

            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz sonucu güncellenirken hata oluştu: {Id}", analysisResult.Id);
            throw new ApplicationException($"Analiz sonucu güncellenirken hata: {ex.Message}", ex);
        }
    }

    public async Task DeleteOldResultsAsync(DateTime cutoffDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var oldResults = await context.AnalysisResults
            .Where(r => r.AnalyzedAt < cutoffDate)
            .ToListAsync();

        context.AnalysisResults.RemoveRange(oldResults);
        await context.SaveChangesAsync();
    }

    public async Task<List<AnalysisResult>> GetUserResultsAsync(string userId, TimeSpan period)
    {
        var startDate = DateTime.UtcNow.Subtract(period);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AnalysisResults
            .Include(r => r.RiskFactors)
            .Where(r => r.AnalyzedAt >= startDate)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync();
    }
}