namespace Analiz.Domain.Entities;

public class GenericPrediction
{
    public bool PredictedLabel { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}