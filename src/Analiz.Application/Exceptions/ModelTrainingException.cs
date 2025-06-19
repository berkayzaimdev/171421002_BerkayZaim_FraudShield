namespace Analiz.Application.Exceptions;

public class ModelTrainingException : Exception
{
    public ModelTrainingException(string message, Exception innerException = null)
        : base(message, innerException)
    {
    }
}