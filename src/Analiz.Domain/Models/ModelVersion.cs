using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class ModelVersion
{
    public string Version { get; set; }
    public ModelStatus Status { get; set; }
    public DateTime TrainedAt { get; set; }
    public Dictionary<string, double> Metrics { get; set; }
}