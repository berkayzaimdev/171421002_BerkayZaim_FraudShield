using Analiz.Domain;
using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces;

public interface IRiskService
{
    Task<RiskEvaluation> EvaluateRiskAsync(TransactionData data);
    Task<RiskScore> DetermineRiskScoreAsync(double fraudProbability, double anomalyScore);
    Task<bool> IsHighRiskTransactionAsync(TransactionData data);
    Task<List<RiskFactor>> IdentifyRiskFactorsAsync(TransactionData data, ModelPrediction prediction);
    Task<Dictionary<string, double>> CalculateFeatureImportanceAsync(TransactionData data, ModelPrediction prediction);
    Task<RiskProfile> GetUserRiskProfileAsync(Guid userId);
}