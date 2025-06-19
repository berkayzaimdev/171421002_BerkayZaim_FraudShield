using Analiz.Domain.Entities;
using Analiz.Domain.Entities.Rule;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.Interfaces;

/// <summary>
/// Fraud kural servisi interface'i
/// </summary>
public interface IFraudRuleService
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
    /// Kural oluştur
    /// </summary>
    Task<FraudRule> CreateRuleAsync(FraudRuleCreateModel model, string createdBy);

    /// <summary>
    /// Kural güncelle
    /// </summary>
    Task<FraudRule> UpdateRuleAsync(Guid id, FraudRuleUpdateModel model, string modifiedBy);

    /// <summary>
    /// Kuralı aktifleştir
    /// </summary>
    Task<FraudRule> ActivateRuleAsync(Guid id, string modifiedBy);

    /// <summary>
    /// Kuralı deaktif et
    /// </summary>
    Task<FraudRule> DeactivateRuleAsync(Guid id, string modifiedBy);

    /// <summary>
    /// Kuralı test moduna al
    /// </summary>
    Task<FraudRule> SetRuleTestModeAsync(Guid id, string modifiedBy);

    /// <summary>
    /// Kural sil
    /// </summary>
    Task<bool> DeleteRuleAsync(Guid id);

    /// <summary>
    /// Kurallardaki değişiklikleri uygula (cache yenileme vb.)
    /// </summary>
    Task ApplyRuleChangesAsync();
}