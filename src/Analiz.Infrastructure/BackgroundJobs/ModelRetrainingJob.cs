using Analiz.Application.Interfaces;

namespace Analiz.Infrastructure.BackgroundJobs;

/*public class ModelRetrainingJob : IJob
{
    private readonly IModelService _modelService;
    private readonly IFeatureExtractionService _featureExtractor;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<ModelRetrainingJob> _logger;
    private readonly ModelRetrainingConfiguration _config;

    public ModelRetrainingJob(
        IModelService modelService,
        IFeatureExtractionService featureExtractor,
        ITransactionRepository transactionRepository,
        ILogger<ModelRetrainingJob> logger,
        IOptions<ModelRetrainingConfiguration> config)
    {
        _modelService = modelService;
        _featureExtractor = featureExtractor;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _config = config.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting model retraining job at {time}", DateTime.UtcNow);

            // Son dönem işlemlerini al
            var recentTransactions = await _transactionRepository
                .GetTransactionsBetweenDatesAsync(
                    DateTime.UtcNow.AddDays(-_config.TrainingDataDays),
                    DateTime.UtcNow);

            // Feature extraction
            var features = await _featureExtractor
                .ExtractBatchFeaturesAsync(recentTransactions.Select(t => t.ToTransactionData()).ToList());

            // Her model için retraining
            foreach (var modelConfig in _config.ModelsToRetrain)
            {
                await RetrainModelAsync(modelConfig, features, recentTransactions);
            }

            _logger.LogInformation("Model retraining job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during model retraining job");
            throw;
        }
    }

    private async Task RetrainModelAsync(
        ModelRetrainingConfig modelConfig,
        List<FeatureSet> features,
        List<Transaction> transactions)
    {
        try
        {
            var trainingRequest = new TrainingRequest
            {
                ModelName = modelConfig.ModelName,
                ModelType = modelConfig.ModelType,
                TrainingData = transactions.Select(t => t.ToTransactionData()).ToList(),
                Configuration = modelConfig.Configuration
            };

            var result = await _modelService.TrainModelAsync(trainingRequest);

            _logger.LogInformation(
                "Model {ModelName} retrained successfully. Metrics: {Metrics}",
                modelConfig.ModelName,
                JsonSerializer.Serialize(result.Metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retraining model {ModelName}", modelConfig.ModelName);
            throw;
        }
    }
}
*/