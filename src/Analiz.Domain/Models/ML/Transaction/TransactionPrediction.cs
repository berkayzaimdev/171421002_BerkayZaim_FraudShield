namespace Analiz.Domain.Entities.ML.Transaction;

public class TransactionPrediction
{
    public bool PredictedLabel { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }
}