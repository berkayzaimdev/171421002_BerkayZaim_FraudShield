using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Repositories;

public interface IAnalysisResultRepository
{
    Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId);
    Task<AnalysisResult?> GetByTransactionIdAsync(Guid transactionId);
    Task<List<AnalysisResult>> GetByTransactionIdsAsync(List<Guid> transactionIds);
    Task<List<AnalysisResult>> GetResultsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<AnalysisResult>> GetHighRiskResultsAsync(TimeSpan period);
    Task<AnalysisResult> SaveResultAsync(AnalysisResult result);
    Task DeleteOldResultsAsync(DateTime cutoffDate);
    Task<List<AnalysisResult>> GetUserResultsAsync(string userId, TimeSpan period);
    Task<AnalysisResult> UpdateAsync(AnalysisResult analysisResult);
}