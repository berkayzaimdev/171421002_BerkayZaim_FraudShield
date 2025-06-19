using Microsoft.ML;

namespace Analiz.Domain.Entities.ML;

public class Ensemble
{
    public class LightGBMPrediction
    {
        public bool PredictedLabel { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }

    public class PCAPredictionOutput
    {
        public bool IsAnomaly { get; set; }
        public float AnomalyScore { get; set; }
        public float Probability { get; set; }
        public bool PredictedLabel { get; set; }
        public float Score { get; set; }
    }

    // Ensemble tahmin sonucu
    public class EnsemblePrediction
    {
        public float LightGBMProbability { get; set; }
        public float PCAPredictionProbability { get; set; }
        public float EnsembleProbability { get; set; }
        public bool EnsembleLabel { get; set; }
    }

    public class EnsembleModelResults
    {
        public TrainingResult LightGBMResult { get; set; }
        public TrainingResult PCAResult { get; set; }
        public TrainingResult EnsembleResult { get; set; }
    }
}