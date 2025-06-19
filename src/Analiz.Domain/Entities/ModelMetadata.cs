using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Analiz.Domain.Events;
using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class ModelMetadata : Entity
{
    public string ModelName { get; private set; }
    public string Version { get; private set; }
    public ModelType Type { get; private set; }

    [NotMapped] public Dictionary<string, double> Metrics { get; private set; }

    public string MetricsJson
    {
        get => Metrics != null ? JsonSerializer.Serialize(Metrics) : null;
        private set => Metrics = !string.IsNullOrEmpty(value)
            ? JsonSerializer.Deserialize<Dictionary<string, double>>(value)
            : new Dictionary<string, double>();
    }

    public ModelStatus Status { get; private set; }
    public string Configuration { get; private set; }
    public DateTime TrainedAt { get; private set; }
    public DateTime? LastUsedAt { get; set; }

    private ModelMetadata()
    {
        Metrics = new Dictionary<string, double>();
    }

    public static ModelMetadata Create(
        string modelName,
        string version,
        ModelType type,
        string configuration)
    {
        return new ModelMetadata
        {
            Id = Guid.NewGuid(),
            ModelName = modelName,
            Version = version,
            Type = type,
            Status = ModelStatus.Training,
            Configuration = configuration,
            TrainedAt = DateTime.UtcNow,
            Metrics = new Dictionary<string, double>(),
            LastModifiedBy = "system",
        };
    }

    public void UpdateMetrics(Dictionary<string, double> metrics)
    {
        Metrics = metrics;
        MetricsJson = JsonSerializer.Serialize(metrics);
        LastUsedAt = DateTime.UtcNow;
        AddDomainEvent(new ModelMetricsUpdatedEvent(Id, metrics));
    }

    public void Activate()
    {
        Status = ModelStatus.Active;
        AddDomainEvent(new ModelActivatedEvent(Id, ModelName, Version));
    }

    public void Deactivate()
    {
        Status = ModelStatus.Inactive;
        AddDomainEvent(new ModelDeactivatedEvent(Id, ModelName, Version));
    }

    public void MarkAsFailed(string error)
    {
        Status = ModelStatus.Failed;
        AddDomainEvent(new ModelFailedEvent(Id, ModelName, Version, error));
    }

    public void StartTraining()
    {
        Status = ModelStatus.Training;
        AddDomainEvent(new ModelTrainingStartedEvent(Id, ModelName, Version));
    }

    public void UpdateConfiguration(string configuration)
    {
        Configuration = configuration;
        LastUsedAt = DateTime.UtcNow;
    }
}