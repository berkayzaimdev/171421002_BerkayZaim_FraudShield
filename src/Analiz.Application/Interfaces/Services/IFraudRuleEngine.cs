using Analiz.Application.DTOs.Response;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.Rule.Context;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.Interfaces.Services;

/// <summary>
/// Kural değerlendirme motoru
/// </summary>
public interface IFraudRuleEngine
{
    Task<List<RuleResult>> EvaluateRulesAsync(TransactionData transactionData);

    /// <summary>
    /// Tüm kuralları değerlendir
    /// </summary>
    Task<List<RuleEvaluationResult>> EvaluateAllRulesAsync(RuleEvaluationContext context);

    /// <summary>
    /// Belirli bir kategoriyi değerlendir
    /// </summary>
    Task<List<RuleEvaluationResult>> EvaluateRulesByCategoryAsync(RuleCategory category, RuleEvaluationContext context);

    /// <summary>
    /// Kuralları yeniden yükle
    /// </summary>
    Task ReloadRulesAsync();
}