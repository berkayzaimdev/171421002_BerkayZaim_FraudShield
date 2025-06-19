using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Analiz.Domain.Events;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Entities;

public class FeatureConfiguration : Entity
{
    public Dictionary<string, bool> EnabledFeatures { get; private set; }
    public Dictionary<string, FeatureSetting> FeatureSettings { get; private set; }
    public string Version { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    [NotMapped] public Dictionary<string, double> NormalizationParameters { get; private set; }

    public string NormalizationParametersJson
    {
        get => NormalizationParameters != null ? JsonSerializer.Serialize(NormalizationParameters) : null;
        private set => NormalizationParameters = !string.IsNullOrEmpty(value)
            ? JsonSerializer.Deserialize<Dictionary<string, double>>(value)
            : new Dictionary<string, double>();
    }

    public FeatureConfiguration()
    {
        EnabledFeatures = new Dictionary<string, bool>();
        FeatureSettings = new Dictionary<string, FeatureSetting>();
        NormalizationParameters = new Dictionary<string, double>();
    }

    public static FeatureConfiguration Create(
        Dictionary<string, bool> enabledFeatures,
        Dictionary<string, FeatureSetting> featureSettings,
        Dictionary<string, double> normalizationParameters)
    {
        return new FeatureConfiguration
        {
            Id = Guid.NewGuid(),
            EnabledFeatures = enabledFeatures,
            FeatureSettings = featureSettings,
            NormalizationParameters = normalizationParameters,
            Version = GenerateVersion(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateFeatures(Dictionary<string, bool> features)
    {
        EnabledFeatures = features;
        Version = GenerateVersion();
        AddDomainEvent(new FeatureConfigurationUpdatedEvent(Id, Version));
    }

    public void UpdateSettings(Dictionary<string, FeatureSetting> settings)
    {
        FeatureSettings = settings;
        Version = GenerateVersion();
        AddDomainEvent(new FeatureSettingsUpdatedEvent(Id, Version));
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            AddDomainEvent(new FeatureConfigurationDeactivatedEvent(Id, Version));
        }
    }

    private static string GenerateVersion()
    {
        return $"v{DateTime.UtcNow:yyyyMMdd.HHmmss}";
    }
}