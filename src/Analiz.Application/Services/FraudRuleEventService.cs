using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.Rule;
using Analiz.Domain.Models.Rule;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

/// <summary>
/// Fraud olay servisi
/// </summary>
public class FraudRuleEventService : IFraudRuleEventService
{
    private readonly IFraudRuleEventRepository _fraudRuleEventRepository;
    private readonly ILogger<FraudRuleEventService> _logger;

    public FraudRuleEventService(
        IFraudRuleEventRepository fraudRuleEventRepository,
        ILogger<FraudRuleEventService> logger)
    {
        _fraudRuleEventRepository = fraudRuleEventRepository ?? throw new ArgumentNullException(nameof(fraudRuleEventRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetAllEventsAsync()
    {
        try
        {
            _logger.LogInformation("Getting all fraud rule events");
            return await _fraudRuleEventRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all fraud rule events");
            throw;
        }
    }

    /// <summary>
    /// Çözülmemiş olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetUnresolvedEventsAsync()
    {
        try
        {
            _logger.LogInformation("Getting unresolved fraud rule events");
            return await _fraudRuleEventRepository.GetUnresolvedEventsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unresolved fraud rule events");
            throw;
        }
    }

    /// <summary>
    /// Hesap ID'sine göre olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetEventsByAccountIdAsync(Guid accountId)
    {
        try
        {
            _logger.LogInformation("Getting fraud rule events for account {AccountId}", accountId);
            return await _fraudRuleEventRepository.GetByAccountIdAsync(accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud rule events for account {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// IP adresine göre olayları getir
    /// </summary>
    public async Task<IEnumerable<FraudRuleEvent>> GetEventsByIpAddressAsync(string ipAddress)
    {
        try
        {
            _logger.LogInformation("Getting fraud rule events for IP address {IpAddress}", ipAddress);
            return await _fraudRuleEventRepository.GetByIpAddressAsync(ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud rule events for IP address {IpAddress}", ipAddress);
            throw;
        }
    }

    /// <summary>
    /// ID'ye göre olay getir
    /// </summary>
    public async Task<FraudRuleEvent> GetEventByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting fraud rule event with ID {EventId}", id);
            return await _fraudRuleEventRepository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fraud rule event with ID {EventId}", id);
            throw;
        }
    }

    /// <summary>
    /// Olayı çöz
    /// </summary>
    public async Task<FraudRuleEvent> ResolveEventAsync(Guid id, FraudEventResolveModel model, string resolvedBy)
    {
        try
        {
            _logger.LogInformation("Resolving fraud rule event {EventId} by {ResolvedBy}", id, resolvedBy);
            
            var fraudEvent = await _fraudRuleEventRepository.GetByIdAsync(id);
            if (fraudEvent == null)
            {
                throw new KeyNotFoundException($"Fraud rule event with ID {id} not found");
            }

            // Olayı çöz
            fraudEvent.Resolve(resolvedBy, model.ResolutionNotes);

            // Değişiklikleri kaydet
            await _fraudRuleEventRepository.UpdateAsync(fraudEvent);

            _logger.LogInformation("Fraud rule event {EventId} resolved successfully", id);
            return fraudEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving fraud rule event {EventId}", id);
            throw;
        }
    }
} 