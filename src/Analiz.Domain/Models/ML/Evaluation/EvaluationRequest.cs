using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities.ML.Evaluation;

public record EvaluationRequest
{
    public required string ModelName { get; init; }
    public required string Version { get; init; }
    public required List<TransactionData> EvaluationData { get; init; }
    public required ModelType ModelType { get; init; }
    public required List<bool> Labels { get; init; }

    // Validation
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ModelName)
               && !string.IsNullOrEmpty(Version)
               && EvaluationData != null
               && EvaluationData.Any()
               && Labels != null
               && Labels.Count == EvaluationData.Count;
    }
}