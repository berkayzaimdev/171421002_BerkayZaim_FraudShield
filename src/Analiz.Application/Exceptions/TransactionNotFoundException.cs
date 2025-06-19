namespace Analiz.Application.Exceptions;

public class TransactionNotFoundException : Exception
{
    public TransactionNotFoundException(Guid transactionId)
        : base($"Transaction not found with ID: {transactionId}")
    {
    }
}