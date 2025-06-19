using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Repositories;

public interface IRiskEvaluationRepository
{
    Task<RiskEvaluation?> GetByIdAsync(Guid id);
    Task<IEnumerable<RiskEvaluation>> GetByEntityAsync(string entityType, Guid entityId);
    Task<IEnumerable<RiskEvaluation>> GetByEvaluationTypeAsync(string evaluationType, DateTime? fromDate = null);
    Task<IEnumerable<RiskEvaluation>> GetLatestEvaluationsAsync(string entityType, Guid entityId, int count = 1);
    Task<IEnumerable<RiskEvaluation>> GetByTransactionIdsAsync(List<Guid> transactionIds);
    Task<RiskEvaluation> CreateAsync(RiskEvaluation evaluation);
    Task<RiskEvaluation> UpdateAsync(RiskEvaluation evaluation);
    Task DeleteAsync(Guid id, string deletedBy);
    Task<bool> ExistsAsync(Guid id);
    Task<RiskEvaluation?> GetLatestRiskDataAsync(string entityType, Guid entityId);
} 