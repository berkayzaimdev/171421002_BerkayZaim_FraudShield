namespace Analiz.Application.Exceptions;

public class ModelEvaluationException : Exception
{
    public ModelEvaluationException(string message, Exception innerException = null)
        : base(message, innerException)
    {
    }
}