namespace Analiz.Domain.Entities.ML.DataSet;

public class BinaryPrediction
{
    public bool PredictedLabel { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }
}