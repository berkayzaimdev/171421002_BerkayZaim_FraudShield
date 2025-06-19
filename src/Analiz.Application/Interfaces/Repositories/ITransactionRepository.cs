using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> GetTransactionAsync(Guid transactionId);
    Task<List<Transaction>> GetUserTransactionsAsync(string userId, TimeSpan period);
    Task<List<Transaction>> GetTransactionsBetweenDatesAsync(DateTime startDate, DateTime endDate);

    Task<Transaction> SaveTransactionAsync(Transaction transaction);
    Task<int> GetTransactionCountAsync(string userId, DateTime startDate, DateTime endDate);

    //Task<int> ArchiveTransactionsAsync(DateTime cutoffDate);
    Task UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status);
    Task<bool> ExistsAsync(Guid transactionId);
    Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId);
    Task<List<FraudAlert>> GetActiveAlertsAsync();
    Task<List<Transaction>> GetUserTransactionHistoryAsync(string userId, int limit);
}