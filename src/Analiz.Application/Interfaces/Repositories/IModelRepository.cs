using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.ML;

namespace Analiz.Application.Interfaces.Repositories;

public interface IModelRepository
{
    Task<ModelMetadata> GetModelAsync(string modelName, string version);
    Task<ModelMetadata> GetActiveModelAsync(string modelName);
    Task<ModelMetadata> GetActiveModelAsync(ModelType type);
    Task<ModelMetadata> FindByNameAsync(string modelName);
    Task<List<ModelVersion>> GetModelVersionsAsync(string modelName);
    Task<List<ModelMetadata>> GetAllModelsAsync();
        
    // Model metadata işlemleri
    Task SaveModelMetadataAsync(ModelMetadata modelMetadata);
    Task UpdateModelMetadataAsync(ModelMetadata modelMetadata);
        
    // Model dosyası işlemleri
    Task SaveModelFileAsync(Guid modelId, string modelFilePath);
    Task<string> GetModelFilePath(Guid modelId);
    Task<string> GetModelInfoPath(Guid modelId);
        
    // Geriye dönük uyumluluk için
    Task SaveModelAsync(ModelMetadata modelMetadata, ITransformer model);
    Task<ITransformer> LoadModelTransformerAsync(Guid modelId);
}