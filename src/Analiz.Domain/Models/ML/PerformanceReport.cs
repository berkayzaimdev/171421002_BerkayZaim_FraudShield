namespace Analiz.Domain.Entities.ML;

public class PerformanceReport
{
    public string ModelName { get; set; }
    public string Version { get; set; }
    public ModelMetrics CurrentMetrics { get; set; }
    public List<ModelMetrics> HistoricalMetrics { get; set; }
    public Dictionary<string, double> DriftMetrics { get; set; }
    public DateTime GeneratedAt { get; set; }

    public PerformanceReport()
    {
        HistoricalMetrics = new List<ModelMetrics>();
        DriftMetrics = new Dictionary<string, double>();
    }
}