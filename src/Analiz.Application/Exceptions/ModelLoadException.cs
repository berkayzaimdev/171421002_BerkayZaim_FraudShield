namespace Analiz.Application.Exceptions;

public class ModelLoadException : Exception
{
    public ModelLoadException(string message, Exception innerException = null)
        : base(message, innerException)
    {
    }
}