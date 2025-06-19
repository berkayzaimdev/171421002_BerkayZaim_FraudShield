// Analiz.Persistence/Repositories/ModelRepository.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Analiz.Persistence.Repositories
{
    public class ModelRepository : IModelRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ModelRepository> _logger;
        private readonly string _modelsPath;

        public ModelRepository(
            ApplicationDbContext dbContext,
            ILogger<ModelRepository> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _modelsPath = configuration["ML:Python:ModelsPath"] ?? 
                          Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Models");
            
            Directory.CreateDirectory(_modelsPath);
        }

        public async Task<ModelMetadata> GetModelAsync(string modelName, string version)
        {
            return await _dbContext.Models
                .FirstOrDefaultAsync(m => m.ModelName == modelName && m.Version == version);
        }

        public async Task<ModelMetadata> GetActiveModelAsync(string modelName)
        {
            return await _dbContext.Models
                .FirstOrDefaultAsync(m => m.ModelName == modelName && m.Status == ModelStatus.Active);
        }
        public async Task<ModelMetadata> GetActiveModelAsync(ModelType type)
        {
            return await _dbContext.Models
                .FirstOrDefaultAsync(m => m.Type == type && m.Status == ModelStatus.Active);
        }


        public async Task<ModelMetadata> FindByNameAsync(string modelName)
        {
            return await _dbContext.Models
                .Where(m => m.ModelName == modelName)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ModelVersion>> GetModelVersionsAsync(string modelName)
        {
            var models = await _dbContext.Models
                .Where(m => m.ModelName == modelName)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return models.Select(m => new ModelVersion
                {
                    Version = m.Version,
                    TrainedAt = m.CreatedAt,
                    Status = m.Status
                })
                .ToList();
        }

        public async Task<List<ModelMetadata>> GetAllModelsAsync()
        {
            try
            {
                var models = await _dbContext.Models
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Veritabanından {Count} model getirildi", models.Count);
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm modeller getirilirken veritabanı hatası oluştu");
                throw;
            }
        }

        public async Task SaveModelMetadataAsync(ModelMetadata modelMetadata)
        {
            await _dbContext.Models.AddAsync(modelMetadata);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Model metadata kaydedildi: {ModelName}, Version: {Version}", 
                modelMetadata.ModelName, modelMetadata.Version);
        }

        public async Task UpdateModelMetadataAsync(ModelMetadata modelMetadata)
        {
            _dbContext.Models.Update(modelMetadata);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Model metadata güncellendi: {ModelName}, Version: {Version}", 
                modelMetadata.ModelName, modelMetadata.Version);
        }

        public async Task SaveModelFileAsync(Guid modelId, string sourceFilePath)
        {
            try
            {
                // Model ID'sine göre hedef klasör oluştur
                var modelDir = Path.Combine(_modelsPath, modelId.ToString());
                Directory.CreateDirectory(modelDir);
                
                // Kaynak dosyayı hedef klasöre kopyala
                var fileName = Path.GetFileName(sourceFilePath);
                var targetPath = Path.Combine(modelDir, fileName);
                
                // Dosya zaten varsa üzerine yaz
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                
                File.Copy(sourceFilePath, targetPath);
                
                _logger.LogInformation("Model dosyası kaydedildi: {SourcePath} -> {TargetPath}", 
                    sourceFilePath, targetPath);
                
                // Model info dosyasını da kopyala
                var sourceDir = Path.GetDirectoryName(sourceFilePath);
                var infoFiles = Directory.GetFiles(sourceDir, "model_info_*.json");
                
                if (infoFiles.Length > 0)
                {
                    var infoFile = infoFiles[0];
                    var infoFileName = Path.GetFileName(infoFile);
                    var targetInfoPath = Path.Combine(modelDir, infoFileName);
                    
                    // Info dosyası zaten varsa üzerine yaz
                    if (File.Exists(targetInfoPath))
                    {
                        File.Delete(targetInfoPath);
                    }
                    
                    File.Copy(infoFile, targetInfoPath);
                    
                    _logger.LogInformation("Model info dosyası kaydedildi: {InfoPath}", targetInfoPath);
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model dosyası kaydedilirken hata: {ModelId}", modelId);
                throw;
            }
        }

        public async Task<string> GetModelFilePath(Guid modelId)
        {
            try
            {
                var modelDir = Path.Combine(_modelsPath, modelId.ToString());
                if (!Directory.Exists(modelDir))
                {
                    _logger.LogWarning("Model dizini bulunamadı: {ModelDir}", modelDir);
                    return null;
                }
                
                // .joblib uzantılı model dosyasını bul
                var modelFiles = Directory.GetFiles(modelDir, "*.joblib");
                if (modelFiles.Length == 0)
                {
                    // .pkl uzantısını da dene
                    modelFiles = Directory.GetFiles(modelDir, "*.pkl");
                }
                
                if (modelFiles.Length == 0)
                {
                    _logger.LogWarning("Model dosyası bulunamadı: {ModelDir}", modelDir);
                    return null;
                }
                
                return modelFiles[0];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model dosya yolu alınırken hata: {ModelId}", modelId);
                return null;
            }
        }

        public async Task<string> GetModelInfoPath(Guid modelId)
        {
            try
            {
                var modelDir = Path.Combine(_modelsPath, modelId.ToString());
                if (!Directory.Exists(modelDir))
                {
                    _logger.LogWarning("Model dizini bulunamadı: {ModelDir}", modelDir);
                    return null;
                }
                
                // model_info dosyasını bul
                var infoFiles = Directory.GetFiles(modelDir, "model_info_*.json");
                if (infoFiles.Length == 0)
                {
                    _logger.LogWarning("Model bilgi dosyası bulunamadı: {ModelDir}", modelDir);
                    return null;
                }
                
                return infoFiles[0];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model bilgi dosyası yolu alınırken hata: {ModelId}", modelId);
                return null;
            }
        }

        // Geriye dönük uyumluluk için ML.NET metodları
        public async Task SaveModelAsync(ModelMetadata modelMetadata, ITransformer model)
        {
            _logger.LogWarning("ML.NET SaveModelAsync kullanılıyor, Python entegrasyonu için SaveModelFileAsync kullanın");
            
            // Metadata'yı kaydet
            await _dbContext.Models.AddAsync(modelMetadata);
            await _dbContext.SaveChangesAsync();
            
            // Model'i dosya olarak kaydet
            var modelDir = Path.Combine(_modelsPath, modelMetadata.Id.ToString());
            Directory.CreateDirectory(modelDir);
            
            var modelPath = Path.Combine(modelDir, $"{modelMetadata.ModelName}_{modelMetadata.Version}.zip");
            using var fs = File.Create(modelPath);
            // ML.NET model save - artık kullanılmıyor
        }

        public async Task<ITransformer> LoadModelTransformerAsync(Guid modelId)
        {
            _logger.LogWarning("ML.NET LoadModelTransformerAsync kullanılıyor, Python entegrasyonu için PythonMLService kullanın");
            
            // Bu metod artık desteklenmiyor, boş transformer döndür
            return null;
        }
    }
}