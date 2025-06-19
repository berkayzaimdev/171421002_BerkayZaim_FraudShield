using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Repositories;

public interface IFeatureConfigurationRepository
{
    Task<FeatureConfiguration> GetActiveConfigurationAsync();
    Task<FeatureConfiguration> UpdateOrCreateAsync(FeatureConfiguration configuration);
}