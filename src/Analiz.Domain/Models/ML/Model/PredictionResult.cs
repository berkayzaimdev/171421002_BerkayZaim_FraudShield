using Microsoft.ML.Data;

namespace Analiz.Domain.Entities.ML;

public class PredictionResult
{
    [ColumnName("Label")] public bool Label { get; set; }

    [ColumnName("PredictedLabel")] public bool PredictedLabel { get; set; }

    [ColumnName("Score")] public float Score { get; set; }

    [ColumnName("Probability")] public float Probability { get; set; }
}