using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.Evaluation;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.ML;

namespace Analiz.Application.Interfaces;

public interface IModelService
{
    Task<TrainingResult> TrainModelAsync(TrainingRequest request);
    Task<TrainingResult> TrainEnsembleModelAsync(TrainingRequest request);
    Task<EvaluationResult> EvaluateModelAsync(EvaluationRequest request);
    Task<ModelMetrics> GetModelMetricsAsync(string modelName);
    Task<bool> UpdateModelAsync(string modelName, ModelUpdateRequest request);
    Task<List<ModelVersion>> GetModelVersionsAsync(string modelName);
    Task<List<ModelMetadata>> GetAllModelsAsync();
    Task<bool> ActivateModelVersionAsync(string modelName, string version);
    Task<bool> UpdateModelStatusAsync(Guid modelId, string status);
    Task<ITransformer> GetModelTransformerAsync(string modelName);

    Task<ModelPrediction> PredictAsync(string modelName, ModelInput input, ModelType type);
    Task<ModelPrediction> PredictAsync(string modelName, PCAModelInput pcaInput);
    Task<ModelPrediction> PredictAdvancedAsync(ModelInput input, string advancedModelType);

    // IModelService interface'ine eklenmeli
    ModelPrediction PredictSingle(ITransformer model, ModelInput input);
    ModelPrediction PredictSinglePCA(ITransformer model, PCAModelInput input);
    Task<(ITransformer Transformer, string ConfigJson)> GetModelWithConfigAsync(string modelName);
}