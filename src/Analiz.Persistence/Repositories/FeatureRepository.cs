using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class FeatureRepository : IFeatureRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FeatureRepository> _logger;

    public FeatureRepository(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<FeatureRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FeatureConfiguration> GetFeatureConfigurationAsync()
    {
        if (_cache.TryGetValue("FeatureConfiguration", out FeatureConfiguration config))
            return config;

        var configuration = await _context.FeatureConfigurations
            .Where(f => f.IsActive)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();

        if (configuration != null) _cache.Set("FeatureConfiguration", configuration, TimeSpan.FromHours(1));

        return configuration;
    }

    public async Task UpdateFeatureConfigurationAsync(FeatureConfiguration configuration)
    {
        // Mevcut aktif konfigürasyonu deaktive et
        var currentActive = await _context.FeatureConfigurations
            .Where(f => f.IsActive)
            .FirstOrDefaultAsync();

        if (currentActive != null)
        {
            currentActive.Deactivate();
            _context.FeatureConfigurations.Update(currentActive);
        }

        await _context.FeatureConfigurations.AddAsync(configuration);
        await _context.SaveChangesAsync();

        _cache.Set("FeatureConfiguration", configuration, TimeSpan.FromHours(1));
    }

    public async Task<List<FeatureImportance>> GetFeatureImportanceHistoryAsync(
        string modelName,
        TimeSpan period)
    {
        var startDate = DateTime.UtcNow.Subtract(period);

        return await _context.FeatureImportance
            .Where(f => f.ModelName == modelName && f.CalculatedAt >= startDate)
            .OrderByDescending(f => f.CalculatedAt)
            .ToListAsync();
    }

    public async Task SaveFeatureImportanceAsync(FeatureImportance importance)
    {
        await _context.FeatureImportance.AddAsync(importance);
        await _context.SaveChangesAsync();
    }

    public async Task<FeatureConfiguration> GetLatestConfigurationAsync()
    {
        return await _context.FeatureConfigurations
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<string>> GetActiveFeatureNamesAsync()
    {
        var config = await GetFeatureConfigurationAsync();
        return config?.EnabledFeatures
            .Where(f => f.Value)
            .Select(f => f.Key)
            .ToList() ?? new List<string>();
    }
}