namespace Analiz.Domain.Entities;

public class ModelTrainingResult
{
    public Guid ModelId { get; set; }
    public Dictionary<string, double> Metrics { get; set; }
    public TimeSpan TrainingTime { get; set; }
}