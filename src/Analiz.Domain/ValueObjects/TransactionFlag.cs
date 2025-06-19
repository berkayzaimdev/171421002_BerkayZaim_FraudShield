using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.ValueObjects;

public class TransactionFlag : ValueObject
{
    public string Code { get; private set; }
    public string Description { get; private set; }
    public FlagSeverity Severity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TransactionFlag()
    {
    }

    public static TransactionFlag Create(
        string code,
        string description,
        FlagSeverity severity)
    {
        return new TransactionFlag
        {
            Code = code,
            Description = description,
            Severity = severity,
            CreatedAt = DateTime.UtcNow
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
        yield return Description;
        yield return Severity;
        yield return CreatedAt;
    }
}