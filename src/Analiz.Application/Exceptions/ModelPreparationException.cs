namespace Analiz.Application.Exceptions;

public class ModelPreparationException : Exception
{
    public ModelPreparationException(string message, Exception innerException = null)
        : base(message, innerException)
    {
    }
}