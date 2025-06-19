namespace Analiz.Domain.Entities.ML.Evaluation;

public class EvaluationResult
{
    public Guid ModelId { get; set; }
    public ModelMetrics Metrics { get; set; }
    public DateTime EvaluationTime { get; set; }
}