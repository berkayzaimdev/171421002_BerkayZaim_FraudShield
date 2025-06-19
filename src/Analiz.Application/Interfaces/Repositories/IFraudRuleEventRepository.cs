using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Repositories;

/// <summary>
/// Fraud olay repository interface'i
/// </summary>
public interface IFraudRuleEventRepository
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
    /// Kural ID'sine göre olayları getir
    /// </summary>
    Task<IEnumerable<FraudRuleEvent>> GetEventsByRuleIdAsync(Guid ruleId);

    /// <summary>
    /// ID'ye göre olay getir
    /// </summary>
    Task<FraudRuleEvent> GetEventByIdAsync(Guid id);

    /// <summary>
    /// Olay ekle
    /// </summary>
    Task<FraudRuleEvent> AddEventAsync(FraudRuleEvent fraudEvent);

    /// <summary>
    /// Olay güncelle
    /// </summary>
    Task<FraudRuleEvent> UpdateEventAsync(FraudRuleEvent fraudEvent);

    // Generic metotlar (service layer tarafından kullanılıyor)
    Task<IEnumerable<FraudRuleEvent>> GetAllAsync();
    Task<FraudRuleEvent> GetByIdAsync(Guid id);
    Task<IEnumerable<FraudRuleEvent>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<FraudRuleEvent>> GetByIpAddressAsync(string ipAddress);
    Task UpdateAsync(FraudRuleEvent fraudEvent);
}