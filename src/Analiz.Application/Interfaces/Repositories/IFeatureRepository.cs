using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Repositories;

public interface IFeatureRepository
{
    Task<FeatureConfiguration> GetFeatureConfigurationAsync();
    Task UpdateFeatureConfigurationAsync(FeatureConfiguration configuration);
    Task<List<FeatureImportance>> GetFeatureImportanceHistoryAsync(string modelName, TimeSpan period);
    Task SaveFeatureImportanceAsync(FeatureImportance importance);
    Task<FeatureConfiguration> GetLatestConfigurationAsync();
    Task<List<string>> GetActiveFeatureNamesAsync();
}