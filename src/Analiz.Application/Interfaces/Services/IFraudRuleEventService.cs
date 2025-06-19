using Analiz.Domain.Entities;
using Analiz.Domain.Entities.Rule;
using Analiz.Domain.Models.Rule;

namespace Analiz.Application.Interfaces.Services;

/// <summary>
/// Fraud olay servisi interface'i
/// </summary>
public interface IFraudRuleEventService
{
    /// <summary>
    /// Tüm olayları getir
    /// </summary>
    Task<IEnumerable<FraudRuleEvent>> GetAllEventsAsync();

    /// <summary>
    /// Çözülmemiş olayları getir
    /// </summary>
    Task<IEnumerable<FraudRuleEvent>> GetUnresolvedEventsAsync();

    /// <summary>
    /// Hesap ID'sine göre olayları getir
    /// </summary>
    Task<IEnumerable<FraudRuleEvent>> GetEventsByAccountIdAsync(Guid accountId);

    /// <summary>
    /// IP adresine göre olayları getir
    /// </summary>
    Task<IEnumerable<FraudRuleEvent>> GetEventsByIpAddressAsync(string ipAddress);

    /// <summary>
    /// ID'ye göre olay getir
    /// </summary>
    Task<FraudRuleEvent> GetEventByIdAsync(Guid id);

    /// <summary>
    /// Olayı çöz
    /// </summary>
    Task<FraudRuleEvent> ResolveEventAsync(Guid id, FraudEventResolveModel model, string resolvedBy);
}