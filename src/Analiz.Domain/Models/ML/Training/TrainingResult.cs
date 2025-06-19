namespace Analiz.Domain.Entities.ML;

public class TrainingResult
{
    public Guid ModelId { get; set; }
    public ModelMetrics Metrics { get; set; }
    public TimeSpan TrainingTime { get; set; }
}