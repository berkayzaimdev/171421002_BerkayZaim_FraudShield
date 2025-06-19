using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.Interfaces.Repositories;

/// <summary>
/// Fraud kural repository interface'i
/// </summary>
public interface IFraudRuleRepository
{
    /// <summary>
    /// Tüm kuralları getir
    /// </summary>
    Task<IEnumerable<FraudRule>> GetAllRulesAsync();

    /// <summary>
    /// Aktif kuralları getir
    /// </summary>
    Task<IEnumerable<FraudRule>> GetActiveRulesAsync();

    /// <summary>
    /// Belirli bir kategorideki kuralları getir
    /// </summary>
    Task<IEnumerable<FraudRule>> GetRulesByCategoryAsync(RuleCategory category);

    /// <summary>
    /// ID'ye göre kural getir
    /// </summary>
    Task<FraudRule> GetRuleByIdAsync(Guid id);

    /// <summary>
    /// Kural koduna göre kural getir
    /// </summary>
    Task<FraudRule> GetRuleByCodeAsync(string ruleCode);

    /// <summary>
    /// Kural ekle
    /// </summary>
    Task<FraudRule> AddRuleAsync(FraudRule rule);

    /// <summary>
    /// Kural güncelle
    /// </summary>
    Task<FraudRule> UpdateRuleAsync(FraudRule rule);

    /// <summary>
    /// Kural sil (genellikle soft delete)
    /// </summary>
    Task<bool> DeleteRuleAsync(Guid id);

    /// <summary>
    /// Cache'i yenile (Redis kullanımı için hazırlık)
    /// </summary>
    Task RefreshCacheAsync();
}