using Microsoft.ML.Data;

namespace Analiz.Domain.Entities.ML;

public class ModelOutput
{
    public bool PredictedLabel { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PCAModelOutput : ModelOutput
{
    [VectorType] public float[] PCAFeatures { get; set; }

    public float AnomalyScore { get; set; }

    [VectorType] public float[] ContributingFeatures { get; set; }
    public bool IsAnomaly { get; set; }

    [NoColumn] // Bu attribute, ML.NET’in bu kolonu görmezden gelmesini sağlar.
    public object Metadata { get; set; }
}

public class PCAPredictionInput
{
    [ColumnName("PCAFeatures")]
    [VectorType(15)]
    public float[] PCAFeatures { get; set; }
}

public class AnomalyPrediction
{
    [ColumnName("Score")] public float Score { get; set; }

    [ColumnName("PredictedLabel")] public bool PredictedLabel { get; set; }
}

// 1.c) Bizim nihai kullanmak istediğimiz çıktı tipi
public class PCAPredictionOutput
{
    [ColumnName("AnomalyScore")] public float AnomalyScore { get; set; }

    [ColumnName("IsAnomaly")] public bool IsAnomaly { get; set; }

    [ColumnName("Score")] public float Score { get; set; }

    [ColumnName("Probability")] public float Probability { get; set; }

    [ColumnName("PredictedLabel")] public bool PredictedLabel { get; set; }
}

// Custom mapping sonucu üretilen çıkış tipi.
// BinaryClassifierEvaluator'ın beklentisi olan Score, Probability ve PredictedLabel alanlarını ekliyoruz.
public class PcaMappingOutput
{
    [VectorType] public float[] PCAFeatures { get; set; }

    public float AnomalyScore { get; set; }

    [VectorType] public float[] ContributingFeatures { get; set; }

    public bool IsAnomaly { get; set; }

    // Değerlendiricinin (evaluator) beklediği sütunlar:
    public float Score { get; set; }
    public float Probability { get; set; }
    public bool PredictedLabel { get; set; }
}

public class PcaOutputCustom : PCAModelOutput
{
    // PCAModelOutput; PCAFeatures, AnomalyScore, ContributingFeatures ve IsAnomaly alanlarını içerir.
}

public class LightGBMModelOutput
{
    public bool PredictedLabel { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }

    [VectorType] public float[] Features { get; set; }
}

public class LightGBMOutput
{
    public bool PredictedLabel { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }

    // Feature importance ve detaylı analiz
    public Dictionary<string, double> FeatureImportances { get; set; }
    public Dictionary<string, double> FeatureContributions { get; set; }

    // Güven metrikleri
    public float ConfidenceScore { get; set; }
    public float UncertaintyScore { get; set; }

    // Açıklanabilirlik
    public List<string> TopContributingFeatures { get; set; }
    public string PredictionExplanation { get; set; }
}