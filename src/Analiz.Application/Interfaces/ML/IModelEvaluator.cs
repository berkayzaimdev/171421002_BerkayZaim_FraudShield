using Analiz.Domain.Entities.ML;
using Microsoft.ML;

namespace Analiz.Application.Interfaces.ML;

public interface IModelEvaluator
{
    Task<ModelMetrics> EvaluateAsync(ITransformer model, IDataView evaluationData);

    Task<Dictionary<string, double>> CalculateMetricsAsync(
        IEnumerable<PredictionResult> predictions,
        IEnumerable<bool> actualLabels);

    Task<PerformanceReport> GeneratePerformanceReportAsync(string modelName);
}