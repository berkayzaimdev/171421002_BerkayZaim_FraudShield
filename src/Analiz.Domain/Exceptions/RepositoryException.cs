using System;

namespace Analiz.Domain.Exceptions;

/// <summary>
/// Repository katmanında oluşan hataları temsil eden exception sınıfı
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException()
    {
    }

    public RepositoryException(string message) : base(message)
    {
    }

    public RepositoryException(string message, Exception innerException) : base(message, innerException)
    {
    }
} 