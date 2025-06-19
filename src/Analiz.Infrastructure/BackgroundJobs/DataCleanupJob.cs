namespace Analiz.Infrastructure.BackgroundJobs;

/*public class DataCleanupJob : IJob
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<DataCleanupJob> _logger;
    private readonly DataRetentionConfiguration _config;

    public DataCleanupJob(
        ITransactionRepository transactionRepository,
        ILogger<DataCleanupJob> logger,
        IOptions<DataRetentionConfiguration> config)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
        _config = config.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting data cleanup job at {time}", DateTime.UtcNow);

            // Eski işlem verilerini arşivle
            var archivedCount = await ArchiveOldTransactionsAsync();

            // Eski analiz sonuçlarını temizle
            var cleanedCount = await CleanupOldAnalysisResultsAsync();

            // Eski metrikleri temizle
            await CleanupOldMetricsAsync();

            _logger.LogInformation(
                "Data cleanup job completed. Archived: {ArchivedCount}, Cleaned: {CleanedCount}",
                archivedCount,
                cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data cleanup job");
            throw;
        }
    }

    private async Task<int> ArchiveOldTransactionsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_config.TransactionRetentionDays);
        return await _transactionRepository.ArchiveTransactionsAsync(cutoffDate);
    }

    private async Task<int> CleanupOldAnalysisResultsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_config.AnalysisResultRetentionDays);
        return await _transactionRepository.DeleteOldAnalysisResultsAsync(cutoffDate);
    }

    private async Task CleanupOldMetricsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_config.MetricsRetentionDays);
        await _transactionRepository.DeleteOldMetricsAsync(cutoffDate);
    }
}*/