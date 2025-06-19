namespace Analiz.Domain.Entities.ML;

public class Response
{
    public class EnsembleTrainingResponse
    {
        public Guid ModelId { get; set; }
        public TimeSpan TrainingTime { get; set; }
        public ModelMetrics Metrics { get; set; }
        public string ModelVersion { get; set; }
    }

    public class ModelTrainingResponse
    {
        public Guid ModelId { get; set; }
        public TimeSpan TrainingTime { get; set; }
        public ModelMetrics Metrics { get; set; }
        public string ModelVersion { get; set; }
    }

    public class ComprehensiveTrainingResponse
    {
        public ModelTrainingResponse LightGBM { get; set; }
        public ModelTrainingResponse PCA { get; set; }
        public ModelTrainingResponse Ensemble { get; set; }
    }

    public class ModelsMetricsResponse
    {
        public Dictionary<string, double> LightGBMMetrics { get; set; }
        public Dictionary<string, double> PCAMetrics { get; set; }
        public Dictionary<string, double> EnsembleMetrics { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}