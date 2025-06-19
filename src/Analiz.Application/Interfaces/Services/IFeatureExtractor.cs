using Analiz.Application.Feature;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Interfaces;

public interface IFeatureExtractionService
{
    Task<FeatureSet> ExtractFeaturesAsync(TransactionData data);
    Task<List<FeatureSet>> ExtractBatchFeaturesAsync(List<TransactionData> data);

    Task<Dictionary<string, float>> ExtractFeaturesAsync(TransactionData data, ModelType modelType);

    Task<List<Dictionary<string, float>>> ExtractBatchFeaturesAsync(List<TransactionData> data, ModelType modelType);

    // Task<FeatureImportance> GetFeatureImportanceAsync(string modelName);
    Task<bool> UpdateFeatureConfigurationAsync(FeatureConfig config);
}