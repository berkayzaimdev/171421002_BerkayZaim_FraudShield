using Analiz.Application.DTOs.Response;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Infrastructure;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

/// <summary>
/// Alert servisi implementasyonu
/// </summary>
public class AlertService : IAlertService
{
    private readonly IFraudAlertRepository _alertRepository;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IFraudAlertRepository alertRepository,
        ILogger<AlertService> logger)
    {
        _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Aktif alert'leri getirir
    /// </summary>
    public async Task<IEnumerable<FraudAlert>> GetActiveAlertsAsync()
    {
        try
        {
            _logger.LogInformation("Aktif alert'ler getiriliyor");
            return await _alertRepository.GetActiveAlertsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif alert'ler getirilirken hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Alert'i çözümler
    /// </summary>
    public async Task ResolveAlertAsync(Guid alertId, string resolution, string resolvedBy)
    {
        try
        {
            _logger.LogInformation("Alert {AlertId} çözümleniyor", alertId);
            
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null)
            {
                throw new KeyNotFoundException($"Alert {alertId} bulunamadı");
            }

            alert.UpdateStatus(AlertStatus.Resolved);
            alert.SetResolvedAt(DateTime.UtcNow);
            alert.SetResolution(resolution);
            alert.SetCreatedBy(resolvedBy);

            await _alertRepository.UpdateAsync(alert);
            
            _logger.LogInformation("Alert {AlertId} başarıyla çözümlendi", alertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert {AlertId} çözümlenirken hata oluştu", alertId);
            throw;
        }
    }

    /// <summary>
    /// Yeni alert oluşturur
    /// </summary>
    public async Task<FraudAlert> CreateAlertAsync(string transactionId, string userId, string type, RiskScore riskScore, List<string> factors)
    {
        try
        {
            _logger.LogInformation("Yeni alert oluşturuluyor. TransactionId: {TransactionId}, UserId: {UserId}", transactionId, userId);
            
            var alert = FraudAlert.Create(
                Guid.Parse(transactionId),
                Guid.Parse(userId),
                riskScore,
                factors);

            await _alertRepository.AddAsync(alert);
            
            _logger.LogInformation("Alert {AlertId} başarıyla oluşturuldu", alert.Id);
            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert oluşturulurken hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Alert'i günceller
    /// </summary>
    public async Task UpdateAlertAsync(FraudAlert alert)
    {
        try
        {
            _logger.LogInformation("Alert {AlertId} güncelleniyor", alert.Id);
            await _alertRepository.UpdateAsync(alert);
            _logger.LogInformation("Alert {AlertId} başarıyla güncellendi", alert.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert {AlertId} güncellenirken hata oluştu", alert.Id);
            throw;
        }
    }

    /// <summary>
    /// Alert'i siler
    /// </summary>
    public async Task DeleteAlertAsync(Guid alertId)
    {
        try
        {
            _logger.LogInformation("Alert {AlertId} siliniyor", alertId);
            await _alertRepository.DeleteAsync(alertId);
            _logger.LogInformation("Alert {AlertId} başarıyla silindi", alertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert {AlertId} silinirken hata oluştu", alertId);
            throw;
        }
    }

    /// <summary>
    /// Kullanıcıya ait alert'leri getirir
    /// </summary>
    public async Task<List<FraudAlert>> GetAlertsByUserIdAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Kullanıcı {UserId} için alert'ler getiriliyor", userId);
            return await _alertRepository.GetByUserIdAsync(Guid.Parse(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı {UserId} için alert'ler getirilirken hata oluştu", userId);
            throw;
        }
    }

    /// <summary>
    /// ID ile alert getirir
    /// </summary>
    public async Task<FraudAlert> GetAlertByIdAsync(Guid alertId)
    {
        try
        {
            _logger.LogInformation("Alert {AlertId} getiriliyor", alertId);
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null)
            {
                throw new KeyNotFoundException($"Alert {alertId} bulunamadı");
            }
            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert {AlertId} getirilirken hata oluştu", alertId);
            throw;
        }
    }

    /// <summary>
    /// Alert'i atar
    /// </summary>
    public async Task<FraudAlert> AssignAlertAsync(Guid alertId, string assignedTo)
    {
        try
        {
            _logger.LogInformation("Alert {AlertId} {AssignedTo} kişisine atanıyor", alertId, assignedTo);
            
            var alert = await _alertRepository.GetByIdAsync(alertId);
            if (alert == null)
            {
                throw new KeyNotFoundException($"Alert {alertId} bulunamadı");
            }

            alert.Assign(assignedTo);
            
            await _alertRepository.UpdateAsync(alert);
            
            _logger.LogInformation("Alert {AlertId} başarıyla {AssignedTo} kişisine atandı", alertId, assignedTo);
            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert {AlertId} atanırken hata oluştu", alertId);
            throw;
        }
    }

    /// <summary>
    /// Durum bazında alert'leri getirir
    /// </summary>
    public async Task<List<FraudAlert>> GetAlertsByStatusAsync(AlertStatus status)
    {
        try
        {
            _logger.LogInformation("Durum {Status} için alert'ler getiriliyor", status);
            return await _alertRepository.GetByStatusAsync(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Durum {Status} için alert'ler getirilirken hata oluştu", status);
            throw;
        }
    }

    /// <summary>
    /// Alert özet bilgilerini getirir
    /// </summary>
    public async Task<AlertSummary> GetAlertSummaryAsync()
    {
        try
        {
            _logger.LogInformation("Alert özet bilgileri getiriliyor");
            
            var allAlerts = await _alertRepository.GetAllAsync();
            
            var resolvedAlerts = allAlerts.Where(a => a.Status == AlertStatus.Resolved && a.ResolvedAt.HasValue).ToList();
            var averageResolutionTime = resolvedAlerts.Any() 
                ? resolvedAlerts.Average(a => (a.ResolvedAt.Value - a.CreatedAt).TotalHours)
                : 0;

            var summary = new AlertSummary
            {
                TotalAlerts = allAlerts.Count,
                ActiveAlerts = allAlerts.Count(a => a.Status == AlertStatus.Active),
                ResolvedAlerts = allAlerts.Count(a => a.Status == AlertStatus.Resolved),
                InvestigatingAlerts = allAlerts.Count(a => a.Status == AlertStatus.Investigating),
                CriticalAlerts = allAlerts.Count(a => a.Type == AlertType.Critical),
                HighAlerts = allAlerts.Count(a => a.Type == AlertType.High),
                MediumAlerts = allAlerts.Count(a => a.Type == AlertType.Medium),
                LowAlerts = allAlerts.Count(a => a.Type == AlertType.Low),
                AverageResolutionTimeHours = averageResolutionTime,
                LastAlertTime = allAlerts.Any() ? allAlerts.Max(a => a.CreatedAt) : DateTime.MinValue
            };

            _logger.LogInformation("Alert özet bilgileri başarıyla oluşturuldu");
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert özet bilgileri getirilirken hata oluştu");
            throw;
        }
    }
}
