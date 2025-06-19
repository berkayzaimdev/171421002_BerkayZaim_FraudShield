using System.Globalization;
using Analiz.Application.Exceptions;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Infrastructure;
using Analiz.Application.Interfaces.Training;
using Analiz.Application.Models.Configuration;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.DataSet;
using Analiz.Domain.Entities.ML.Evaluation;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services.Training;
/*
public class FraudModelTrainingService : IFraudModelTrainingService
{
    private readonly IModelService _modelService;
    private readonly ITestDataService _testDataService;
    private readonly IFeatureExtractionService _extractionService;
    private readonly ILogger<FraudModelTrainingService> _logger;

    public FraudModelTrainingService(
        IModelService modelService,
        ITestDataService testDataService,
        IFeatureExtractionService extractionService,
        ILogger<FraudModelTrainingService> logger)
    {
        _modelService = modelService;
        _testDataService = testDataService;
        _extractionService = extractionService;
        _logger = logger;
    }


    public async Task<(TrainingResult LightGBM, TrainingResult PCA)> TrainModelsAsync()
    {
        try
        {
            _logger.LogInformation("Starting fraud detection model training");

            // Test verisini yükle ve doğrula
            var data = await _testDataService.LoadCreditCardDataAsync();
            ValidateInputData(data);

            _logger.LogInformation("Loaded {Count} records from test data", data.Count);

            // Önce veriyi analiz et
            AnalyzeDataDistribution(data);

            // PCA modeli eğitimi
            var pcaRequest =
                await CreditCardModelConfiguration.CreateTrainingRequest(data, ModelType.PCA, _extractionService,
                    _logger);
            var pcaResult = await _modelService.TrainModelAsync(pcaRequest);

            _logger.LogInformation("PCA model training completed. Metrics: {@Metrics}",
                pcaResult.Metrics);
            // LightGBM modeli eğitimi
            var lightGBMRequest =
                await CreditCardModelConfiguration.CreateTrainingRequest(data, ModelType.LightGBM, _extractionService,
                    _logger);
            var lightGBMResult = await _modelService.TrainModelAsync(lightGBMRequest);

            _logger.LogInformation("LightGBM model training completed. Metrics: {@Metrics}",
                lightGBMResult.Metrics);
            return (lightGBMResult, pcaResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fraud model training");
            throw new ModelTrainingException("Model eğitimi sırasında hata oluştu", ex);
        }
    }

    public async Task<TrainingResult> TrainEnsembleModelAsync()
    {
        try
        {
            _logger.LogInformation("Starting ensemble fraud detection model training");

            // Test verisini yükle ve doğrula
            var data = await _testDataService.LoadCreditCardDataAsync();
            ValidateInputData(data);

            _logger.LogInformation("Loaded {Count} records for ensemble training", data.Count);

            // Ensemble model için training request oluştur
            var ensembleRequest =
                await CreditCardModelConfiguration.CreateEnsembleTrainingRequest(data, _extractionService, _logger);

            // Ensemble modeli eğit
            var ensembleResult = await _modelService.TrainEnsembleModelAsync(ensembleRequest);

            _logger.LogInformation("Ensemble model training completed. Metrics: {@Metrics}",
                ensembleResult.Metrics);

            return ensembleResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ensemble model training");
            throw new ModelTrainingException("Ensemble model eğitimi sırasında hata oluştu", ex);
        }
    }

    public async Task<Ensemble.EnsembleModelResults> TrainAllModelsAsync()
    {
        try
        {
            _logger.LogInformation("Starting comprehensive model training (LightGBM, PCA, and Ensemble)");

            // Tüm modelleri paralel olarak eğit
            var lightGbmTask = TrainModelsAsync();
            var ensembleTask = TrainEnsembleModelAsync();

            // Tüm task'ların tamamlanmasını bekle
            await Task.WhenAll(lightGbmTask, ensembleTask);

            // Sonuçları al
            var (lightGBM, pca) = await lightGbmTask;
            var ensemble = await ensembleTask;

            return new Ensemble.EnsembleModelResults
            {
                LightGBMResult = lightGBM,
                PCAResult = pca,
                EnsembleResult = ensemble
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during comprehensive model training");
            throw new ModelTrainingException("Model eğitimi sırasında hata oluştu", ex);
        }
    }

    private void ValidateInputData(List<CreditCardModelData> data)
    {
        if (data == null || !data.Any())
            throw new ArgumentException("Input data cannot be empty");

        var fraudCount = data.Count(x => x.Label);
        if (fraudCount == 0)
            throw new ArgumentException("Input data must contain fraud cases");

        _logger.LogInformation("Input data validation passed. Total: {Total}, Fraud: {Fraud}",
            data.Count, fraudCount);
    }

    private void AnalyzeDataDistribution(List<CreditCardModelData> data)
    {
        var fraudCount = data.Count(x => x.Label);
        var totalCount = data.Count;
        var fraudRatio = (double)fraudCount / totalCount;

        _logger.LogInformation(
            "Data distribution analysis - Total: {Total}, Fraud: {Fraud}, Ratio: {Ratio:P2}",
            totalCount, fraudCount, fraudRatio);

        if (fraudRatio < 0.001)
            _logger.LogWarning("Very low fraud ratio detected: {Ratio:P4}", fraudRatio);
    }
}*/