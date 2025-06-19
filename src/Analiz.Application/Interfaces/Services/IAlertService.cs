using Analiz.Application.DTOs.Response;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Interfaces.Services;

/// <summary>
/// Alert servisi interface
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Aktif alert'leri getirir
    /// </summary>
    Task<IEnumerable<FraudAlert>> GetActiveAlertsAsync();

    /// <summary>
    /// Alert'i çözümler
    /// </summary>
    Task ResolveAlertAsync(Guid alertId, string resolution, string resolvedBy);

    /// <summary>
    /// Yeni alert oluşturur
    /// </summary>
    Task<FraudAlert> CreateAlertAsync(string transactionId, string userId, string type, RiskScore riskScore, List<string> factors);

    /// <summary>
    /// Alert'i günceller
    /// </summary>
    Task UpdateAlertAsync(FraudAlert alert);

    /// <summary>
    /// Alert'i siler
    /// </summary>
    Task DeleteAlertAsync(Guid alertId);

    Task<List<FraudAlert>> GetAlertsByUserIdAsync(string userId);
    Task<FraudAlert> GetAlertByIdAsync(Guid alertId);
    Task<FraudAlert> AssignAlertAsync(Guid alertId, string assignedTo);
    Task<List<FraudAlert>> GetAlertsByStatusAsync(AlertStatus status);
    Task<AlertSummary> GetAlertSummaryAsync();
}