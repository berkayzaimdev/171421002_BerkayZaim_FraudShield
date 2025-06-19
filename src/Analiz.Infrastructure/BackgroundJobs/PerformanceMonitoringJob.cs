namespace Analiz.Infrastructure.BackgroundJobs;

/*public class PerformanceMonitoringJob : IJob
{
    private readonly IModelEvaluator _modelEvaluator;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMetricsService _metricsService;
    private readonly ILogger<PerformanceMonitoringJob> _logger;
    private readonly PerformanceMonitoringConfiguration _config;

    public PerformanceMonitoringJob(
        IModelEvaluator modelEvaluator,
        ITransactionRepository transactionRepository,
        IMetricsService metricsService,
        ILogger<PerformanceMonitoringJob> logger,
        IOptions<PerformanceMonitoringConfiguration> config)
    {
        _modelEvaluator = modelEvaluator;
        _transactionRepository = transactionRepository;
        _metricsService = metricsService;
        _logger = logger;
        _config = config.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting performance monitoring job at {time}", DateTime.UtcNow);

            // Son dönem performans metriklerini topla
            var performanceMetrics = await CollectPerformanceMetricsAsync();

            // Metrikleri kaydet
            await _metricsService.SaveMetricsAsync(performanceMetrics);

            // Performans alertlerini kontrol et
            await CheckPerformanceAlertsAsync(performanceMetrics);

            _logger.LogInformation("Performance monitoring job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during performance monitoring job");
            throw;
        }
    }

    private async Task<PerformanceMetrics> CollectPerformanceMetricsAsync()
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddHours(-_config.MetricsCollectionHours);

        // İşlem verilerini al
        var transactions = await _transactionRepository
            .GetTransactionsBetweenDatesAsync(startDate, endDate);

        // Model performans metrikleri
        var modelMetrics = await _modelEvaluator
            .CalculateMetricsAsync(
                transactions.Select(t => t.PredictionResult),
                transactions.Select(t => t.IsFraudulent));

        // Sistem performans metrikleri
        var systemMetrics = await CollectSystemMetricsAsync();

        return new PerformanceMetrics
        {
            ModelMetrics = modelMetrics,
            SystemMetrics = systemMetrics,
            CollectedAt = DateTime.UtcNow,
            Period = new DateRange(startDate, endDate)
        };
    }

    private async Task CheckPerformanceAlertsAsync(PerformanceMetrics metrics)
    {
        // Model performans alertleri
        if (metrics.ModelMetrics.AUC < _config.MinimumAUCThreshold)
        {
            await _metricsService.RaiseModelPerformanceAlertAsync(
                "AUC_BELOW_THRESHOLD",
                metrics.ModelMetrics);
        }

        // Sistem performans alertleri
        if (metrics.SystemMetrics.AverageResponseTime > _config.MaxResponseTimeThreshold)
        {
            await _metricsService.RaiseSystemPerformanceAlertAsync(
                "HIGH_RESPONSE_TIME",
                metrics.SystemMetrics);
        }
    }
}*/