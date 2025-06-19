namespace Analiz.Application.Exceptions;

public class RiskScoringException : Exception
{
    public RiskScoringException(string message) : base(message)
    {
    }

    public RiskScoringException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}