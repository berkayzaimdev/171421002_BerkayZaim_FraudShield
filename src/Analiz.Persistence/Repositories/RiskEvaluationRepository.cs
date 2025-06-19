using System.Text.Json;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class RiskEvaluationRepository : IRiskEvaluationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RiskEvaluationRepository> _logger;

    public RiskEvaluationRepository(ApplicationDbContext context, ILogger<RiskEvaluationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RiskEvaluation?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.RiskEvaluations
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk değerlendirmesi getirilirken hata oluştu. Id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<RiskEvaluation>> GetByEntityAsync(string entityType, Guid entityId)
    {
        try
        {
            return await _context.RiskEvaluations
                .Where(x => x.EntityType == entityType && 
                           x.EntityId == entityId )
                .OrderByDescending(x => x.EvaluationTimestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entity için risk değerlendirmeleri getirilirken hata oluştu. EntityType: {EntityType}, EntityId: {EntityId}", 
                entityType, entityId);
            throw;
        }
    }

    public async Task<IEnumerable<RiskEvaluation>> GetByEvaluationTypeAsync(string evaluationType, DateTime? fromDate = null)
    {
        try
        {
            var query = _context.RiskEvaluations
                .Where(x => x.EvaluationType == evaluationType );

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.EvaluationTimestamp >= fromDate.Value);
            }

            return await query
                .OrderByDescending(x => x.EvaluationTimestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Değerlendirme tipine göre risk değerlendirmeleri getirilirken hata oluştu. EvaluationType: {EvaluationType}", 
                evaluationType);
            throw;
        }
    }

    public async Task<IEnumerable<RiskEvaluation>> GetLatestEvaluationsAsync(string entityType, Guid entityId, int count = 1)
    {
        try
        {
            return await _context.RiskEvaluations
                .Where(x => x.EntityType == entityType && 
                           x.EntityId == entityId)
                .OrderByDescending(x => x.EvaluationTimestamp)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Son risk değerlendirmeleri getirilirken hata oluştu. EntityType: {EntityType}, EntityId: {EntityId}, Count: {Count}", 
                entityType, entityId, count);
            throw;
        }
    }

    public async Task<IEnumerable<RiskEvaluation>> GetByTransactionIdsAsync(List<Guid> transactionIds)
    {
        try
        {
            if (!transactionIds.Any()) return new List<RiskEvaluation>();
            
            return await _context.RiskEvaluations
                .Where(x =>  transactionIds.Contains(x.TransactionId))
                .OrderByDescending(x => x.EvaluationTimestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction ID'ler için risk değerlendirmeleri getirilirken hata oluştu. TransactionIds: {TransactionIds}", 
                string.Join(", ", transactionIds));
            throw;
        }
    }

    public async Task<RiskEvaluation> CreateAsync(RiskEvaluation evaluation)
    {
        try
        {
            evaluation.CreatedAt = DateTime.UtcNow;
            evaluation.CreatedBy ??= "system";
            evaluation.IsActive = true;
            evaluation.IsDeleted = false;

            await _context.RiskEvaluations.AddAsync(evaluation);
            await _context.SaveChangesAsync();

            return evaluation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk değerlendirmesi oluşturulurken hata oluştu. EntityType: {EntityType}, EntityId: {EntityId}", 
                evaluation.EntityType, evaluation.EntityId);
            throw;
        }
    }

    public async Task<RiskEvaluation> UpdateAsync(RiskEvaluation evaluation)
    {
        try
        {
            var existingEvaluation = await _context.RiskEvaluations
                .FirstOrDefaultAsync(x => x.Id == evaluation.Id );

            if (existingEvaluation == null)
            {
                throw new KeyNotFoundException($"Risk değerlendirmesi bulunamadı. Id: {evaluation.Id}");
            }

            existingEvaluation.RiskData = evaluation.RiskData;
            existingEvaluation.EvaluationTimestamp = evaluation.EvaluationTimestamp;
            existingEvaluation.EvaluationSource = evaluation.EvaluationSource;
            existingEvaluation.ModelVersion = evaluation.ModelVersion;
            existingEvaluation.IsActive = evaluation.IsActive;
            existingEvaluation.LastModifiedAt = DateTime.UtcNow;
            existingEvaluation.LastModifiedBy = evaluation.LastModifiedBy;

            await _context.SaveChangesAsync();

            return existingEvaluation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk değerlendirmesi güncellenirken hata oluştu. Id: {Id}", evaluation.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, string deletedBy)
    {
        try
        {
            var evaluation = await _context.RiskEvaluations
                .FirstOrDefaultAsync(x => x.Id == id );

            if (evaluation == null)
            {
                throw new KeyNotFoundException($"Risk değerlendirmesi bulunamadı. Id: {id}");
            }

            evaluation.IsDeleted = true;
            evaluation.DeletedAt = DateTime.UtcNow;
            evaluation.DeletedBy = deletedBy;
            evaluation.IsActive = false;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk değerlendirmesi silinirken hata oluştu. Id: {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _context.RiskEvaluations
                .AnyAsync(x => x.Id == id );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk değerlendirmesi varlığı kontrol edilirken hata oluştu. Id: {Id}", id);
            throw;
        }
    }

    public async Task<RiskEvaluation?> GetLatestRiskDataAsync(string entityType, Guid entityId)
    {
        try
        {
            return await _context.RiskEvaluations
                .Where(x => x.EntityType == entityType && 
                           x.EntityId == entityId )
                .OrderByDescending(x => x.EvaluationTimestamp)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Son risk verisi getirilirken hata oluştu. EntityType: {EntityType}, EntityId: {EntityId}", 
                entityType, entityId);
            throw;
        }
    }
} 