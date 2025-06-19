namespace Analiz.Application.Exceptions;

public class ModelMetricsException : Exception
{
    public ModelMetricsException(string message, Exception innerException = null)
        : base(message, innerException)
    {
    }
}