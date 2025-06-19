using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Interfaces.Repositories;

public interface IRiskFactorRepository
{
    Task<RiskFactor> GetByIdAsync(Guid id);
    Task<List<RiskFactor>> GetAllAsync(int limit = 100, int offset = 0);
    Task<List<RiskFactor>> GetAllForTransactionAsync(Guid transactionId);
    Task<List<RiskFactor>> GetByTransactionIdAsync(string transactionId);
    Task<List<RiskFactor>> GetByTypeAsync(RiskFactorType type);
    Task<List<RiskFactor>> GetByUserIdAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<RiskFactor>> GetHighSeverityFactorsAsync(RiskLevel minSeverity, int limit = 100);
    Task<Dictionary<string, int>> GetMostCommonFactorsAsync(string userId, int limit = 10);
    Task<List<RiskFactor>> GetByAnalysisResultIdAsync(Guid analysisResultId);
    Task<bool> AddAsync(RiskFactor riskFactor);
    Task<bool> AddRangeAsync(List<RiskFactor> riskFactors);
    Task<bool> UpdateAsync(RiskFactor riskFactor);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetFactorCountByTypeAsync(string userId, RiskFactorType type);
}