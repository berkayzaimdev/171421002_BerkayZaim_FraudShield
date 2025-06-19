using Analiz.Application.Feature;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Analiz.Application.Services;

public class FeatureEngineeringService : IFeatureExtractionService
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeatureConfigurationRepository _configRepository;
    private readonly ILogger<FeatureEngineeringService> _logger;
    private readonly MLContext _mlContext;

    public FeatureEngineeringService(
        IFeatureRepository featureRepository,
        IFeatureConfigurationRepository configRepository,
        ILogger<FeatureEngineeringService> logger)
    {
        _featureRepository = featureRepository;
        _configRepository = configRepository;
        _logger = logger;
        _mlContext = new MLContext(42);
    }

    /// <summary>
    /// Toplu feature çıkarma işlemi
    /// </summary>
    public async Task<List<Dictionary<string, float>>> ExtractBatchFeaturesAsync(
        List<TransactionData> data,
        ModelType modelType)
    {
        try
        {
            // Aktif konfigürasyonu al veya varsayılanı oluştur
            var config = await _configRepository.GetActiveConfigurationAsync()
                         ?? await CreateAndSaveDefaultConfiguration();

            _logger.LogInformation("Extracting features for {Count} transactions with {Type} model type",
                data.Count, modelType);

            var features = new List<Dictionary<string, float>>();
            foreach (var transaction in data)
            {
                var transactionFeatures = await ExtractFeaturesWithConfig(transaction, config, modelType);
                features.Add(transactionFeatures);
            }

            // Feature önem skorlarını hesapla ve kaydet
            await SaveFeatureImportance(features, modelType);

            return features;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting batch features for {Count} transactions", data.Count);
            throw;
        }
    }

    /// <summary>
    /// Tek bir işlem için feature çıkarma
    /// </summary>
    public async Task<Dictionary<string, float>> ExtractFeaturesAsync(
        TransactionData data,
        ModelType modelType)
    {
        try
        {
            var config = await _configRepository.GetActiveConfigurationAsync();
            if (config == null)
            {
                _logger.LogWarning("No active configuration found, using default");
                config = await CreateAndSaveDefaultConfiguration();
            }

            return await ExtractFeaturesWithConfig(data, config, modelType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting features for transaction {TransactionId}", data.TransactionId);
            throw;
        }
    }

    /// <summary>
    /// Feature konfigürasyonunu günceller
    /// </summary>
    public async Task<bool> UpdateFeatureConfigurationAsync(FeatureConfig config)
    {
        try
        {
            var currentConfig = await _configRepository.GetActiveConfigurationAsync();
            if (currentConfig == null) currentConfig = await CreateAndSaveDefaultConfiguration();

            currentConfig.UpdateFeatures(config.EnabledFeatures);
            currentConfig.UpdateSettings(config.FeatureSettings);

            await _configRepository.UpdateOrCreateAsync(currentConfig);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature configuration");
            return false;
        }
    }

    private async Task<Dictionary<string, float>> ExtractFeaturesWithConfig(
        TransactionData data,
        FeatureConfiguration config,
        ModelType modelType)
    {
        var features = new Dictionary<string, float>();

        // Zaman özellikleri
        if (config.EnabledFeatures.GetValueOrDefault("TimeFeatures", true))
        {
            var timeFeatures = ExtractTimeFeatures(data.Timestamp);
            foreach (var feature in timeFeatures) features[feature.Key] = feature.Value;
        }

        // İşlem özellikleri
        if (config.EnabledFeatures.GetValueOrDefault("TransactionFeatures", true))
        {
            var transactionFeatures = ExtractTransactionFeatures(
                data.Amount,
                config.NormalizationParameters ?? new Dictionary<string, double>()
            );
            foreach (var feature in transactionFeatures) features[feature.Key] = feature.Value;
        }

        // PCA özellikleri
        if (config.EnabledFeatures.GetValueOrDefault("PCAFeatures", true))
        {
            var pcaFeatures = ExtractPCAFeatures(data);
            foreach (var feature in pcaFeatures) features[feature.Key] = feature.Value;
        }

        return features;
    }

    private async Task<FeatureConfiguration> CreateAndSaveDefaultConfiguration()
    {
        _logger.LogInformation("Creating default feature configuration");

        var defaultConfig = FeatureConfiguration.Create(
            new Dictionary<string, bool>
            {
                ["TimeFeatures"] = true,
                ["TransactionFeatures"] = true,
                ["PCAFeatures"] = true
            },
            CreateDefaultFeatureSettings(),
            new Dictionary<string, double>
            {
                ["AmountMin"] = 0,
                ["AmountMax"] = 25000,
                ["TimeScaleFactor"] = 24 * 60 * 60
            });

        // Yeni konfigürasyonu kaydet
        await _configRepository.UpdateOrCreateAsync(defaultConfig);

        return defaultConfig;
    }

    private Dictionary<string, FeatureSetting> CreateDefaultFeatureSettings()
    {
        var settings = new Dictionary<string, FeatureSetting>();

        // Zaman özellikleri için ayarlar
        settings.Add("TimeSin", CreateTimeSetting("TimeSin"));
        settings.Add("TimeCos", CreateTimeSetting("TimeCos"));
        settings.Add("DayFeature", CreateTimeSetting("DayFeature"));
        settings.Add("HourFeature", CreateTimeSetting("HourFeature"));

        // İşlem özellikleri için ayarlar
        settings.Add("Amount", CreateTransactionSetting("Amount", "None"));
        settings.Add("Amount_normalized", CreateTransactionSetting("Amount_normalized", "MinMax"));
        settings.Add("LogAmount", CreateTransactionSetting("LogAmount", "Log"));

        // PCA özellikleri için ayarlar
        for (var i = 1; i <= 28; i++)
        {
            var baseFeature = $"V{i}";
            settings.Add(baseFeature, CreatePCASetting(baseFeature, "None"));
            settings.Add($"{baseFeature}_normalized", CreatePCASetting($"{baseFeature}_normalized", "MinMax"));
        }

        return settings;
    }

    private FeatureSetting CreateTimeSetting(string name)
    {
        return new FeatureSetting
        {
            Name = name,
            Type = FeatureType.Numeric,
            Category = FeatureCategory.Time,
            IsRequired = true,
            TransformationType = name.Contains("Sin") || name.Contains("Cos") ? "Trigonometric" : "None"
        };
    }

    private FeatureSetting CreateTransactionSetting(string name, string transform)
    {
        return new FeatureSetting
        {
            Name = name,
            Type = FeatureType.Numeric,
            Category = FeatureCategory.Transaction,
            IsRequired = true,
            TransformationType = transform
        };
    }

    private FeatureSetting CreatePCASetting(string name, string transform)
    {
        return new FeatureSetting
        {
            Name = name,
            Type = FeatureType.Numeric,
            Category = FeatureCategory.Derived,
            IsRequired = true,
            TransformationType = transform
        };
    }

    private async Task SaveFeatureImportance(
        List<Dictionary<string, float>> features,
        ModelType modelType)
    {
        try
        {
            var statistics = CalculateFeatureStatistics(features);

            foreach (var (feature, stats) in statistics)
            {
                var importance = FeatureImportance.Create(
                    modelType.ToString(),
                    feature,
                    stats["variance"],
                    DetermineFeatureCategory(feature),
                    stats);

                await _featureRepository.SaveFeatureImportanceAsync(importance);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving feature importance scores");
        }
    }


    public Task<FeatureSet> ExtractFeaturesAsync(TransactionData data)
    {
        throw new NotImplementedException();
    }

    public Task<List<FeatureSet>> ExtractBatchFeaturesAsync(List<TransactionData> data)
    {
        throw new NotImplementedException();
    }


    private Dictionary<string, float> ExtractTimeFeatures(DateTime timestamp)
    {
        const double daySeconds = 24 * 60 * 60;
        var timeOfDay = timestamp.TimeOfDay.TotalSeconds;

        return new Dictionary<string, float>
        {
            ["TimeSin"] = (float)Math.Sin(2 * Math.PI * timeOfDay / daySeconds),
            ["TimeCos"] = (float)Math.Cos(2 * Math.PI * timeOfDay / daySeconds),
            ["DayFeature"] = (float)timestamp.DayOfWeek,
            ["HourFeature"] = timestamp.Hour
        };
    }

    private Dictionary<string, float> ExtractTransactionFeatures(
        decimal amount,
        Dictionary<string, double> normParams)
    {
        var amountFloat = (float)amount;
        var normalizedAmount = NormalizeAmount(amountFloat, normParams);

        return new Dictionary<string, float>
        {
            ["Amount"] = amountFloat,
            ["Amount_normalized"] = normalizedAmount,
            ["LogAmount"] = (float)Math.Log(amountFloat + 1)
        };
    }

    /// <summary>
    /// PCA için V1–V28 feature’larını ve normalize edilmiş hallerini çıkarır
    /// </summary>
    private Dictionary<string, float> ExtractPCAFeatures(TransactionData data)
    {
        var features = new Dictionary<string, float>();
        var vFactors = data?.AdditionalData?.VFactors;
        if (vFactors == null || !vFactors.Any())
            return features;

        foreach (var kvp in vFactors)
        {
            var key = kvp.Key; // "V1", "V2", …, "V28"
            var value = kvp.Value;
            // Ham değer
            features[key] = value;
            // Normalize edilmiş değer
            features[$"{key}_normalized"] = NormalizeFeature(value);
        }

        return features;
    }

    private float NormalizeAmount(float amount, Dictionary<string, double> normParams)
    {
        if (normParams.TryGetValue("AmountMin", out var min) &&
            normParams.TryGetValue("AmountMax", out var max))
            return (float)((amount - min) / (max - min));

        return amount;
    }

    private float NormalizeFeature(float value)
    {
        // Simple min-max normalization between -1 and 1
        return Math.Max(-1, Math.Min(1, value));
    }

    private Dictionary<string, Dictionary<string, double>> CalculateFeatureStatistics(
        List<Dictionary<string, float>> features)
    {
        var statistics = new Dictionary<string, Dictionary<string, double>>();

        // Her özellik için istatistikleri hesapla
        foreach (var feature in features.First().Keys)
        {
            var values = features.Select(f => f[feature]).ToList();
            statistics[feature] = new Dictionary<string, double>
            {
                ["mean"] = values.Average(),
                ["variance"] = CalculateVariance(values),
                ["min"] = values.Min(),
                ["max"] = values.Max()
            };
        }

        return statistics;
    }

    private double CalculateVariance(List<float> values)
    {
        var mean = values.Average();
        return values.Average(v => Math.Pow(v - mean, 2));
    }

    private FeatureCategory DetermineFeatureCategory(string featureName)
    {
        if (featureName.StartsWith("Time") || featureName.Contains("Day") || featureName.Contains("Hour"))
            return FeatureCategory.Time;
        if (featureName.StartsWith("V"))
            return FeatureCategory.Derived;
        if (featureName.Contains("Amount"))
            return FeatureCategory.Transaction;

        return FeatureCategory.Derived;
    }
}