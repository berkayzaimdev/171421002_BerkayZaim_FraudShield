using Analiz.Domain;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Interfaces.Services;

public interface IRiskFactorService
{
    Task<List<RiskFactor>> GetTransactionRiskFactorsAsync(Guid transactionId);
    Task<Dictionary<string, int>> GetUserRiskFactorDistributionAsync(Guid userId);
    Task<RiskProfile> CalculateUserRiskProfileAsync(Guid userId);
    Task<bool> AddRiskFactorAsync(RiskFactor riskFactor);
    Task<bool> AddRiskFactorsAsync(List<RiskFactor> riskFactors);
    Task<List<RiskFactor>> IdentifyUserBehaviorRiskFactorsAsync(Guid userId, TransactionData currentTransaction);
    Task<List<RiskFactor>> GetHighRiskFactorsAsync(RiskLevel minSeverity = RiskLevel.High, int limit = 50);
    Task<Dictionary<RiskFactorType, int>> GetRiskFactorTrendAsync(Guid userId, TimeSpan period);
    Task<bool> IsRecurringRiskPatternAsync(Guid userId, RiskFactorType factorType, int threshold = 3);
    Task<string> GenerateRiskFactorReportAsync(Guid transactionId);
}