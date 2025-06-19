using System.Text.Json;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.Rule;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

/// <summary>
/// Fraud kural servisi implementasyonu
/// </summary>
public class FraudRuleService : IFraudRuleService
{
    private readonly IFraudRuleRepository _ruleRepository;
    private readonly ILogger<FraudRuleService> _logger;
    private readonly IFraudRuleEngine _ruleEngine;

    public FraudRuleService(
        IFraudRuleRepository ruleRepository,
        IFraudRuleEngine ruleEngine,
        ILogger<FraudRuleService> logger)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm kuralları getir
    /// </summary>
    public async Task<IEnumerable<FraudRule>> GetAllRulesAsync()
    {
        return await _ruleRepository.GetAllRulesAsync();
    }

    /// <summary>
    /// Aktif kuralları getir
    /// </summary>
    public async Task<IEnumerable<FraudRule>> GetActiveRulesAsync()
    {
        return await _ruleRepository.GetActiveRulesAsync();
    }

    /// <summary>
    /// Belirli bir kategorideki kuralları getir
    /// </summary>
    public async Task<IEnumerable<FraudRule>> GetRulesByCategoryAsync(RuleCategory category)
    {
        return await _ruleRepository.GetRulesByCategoryAsync(category);
    }

    /// <summary>
    /// ID'ye göre kural getir
    /// </summary>
    public async Task<FraudRule> GetRuleByIdAsync(Guid id)
    {
        return await _ruleRepository.GetRuleByIdAsync(id);
    }

    /// <summary>
    /// Kural koduna göre kural getir
    /// </summary>
    public async Task<FraudRule> GetRuleByCodeAsync(string ruleCode)
    {
        return await _ruleRepository.GetRuleByCodeAsync(ruleCode);
    }

    /// <summary>
    /// Kural oluştur
    /// </summary>
    public async Task<FraudRule> CreateRuleAsync(FraudRuleCreateModel model, string createdBy)
    {
        try
        {
            // Model doğrulama
            ValidateRuleModel(model);

            // Configuration JSON'a dönüştürülür
            var configJson = model.Configuration != null
                ? JsonSerializer.Serialize(model.Configuration)
                : "{}";

            // Kural oluştur
            var rule = FraudRule.Create(
                model.Name,
                model.Description,
                model.Category,
                model.Type,
                model.ImpactLevel,
                model.Actions,
                model.ActionDuration,
                model.Priority,
                model.Condition,
                configJson,
                createdBy);

            // Veritabanına kaydet
            var savedRule = await _ruleRepository.AddRuleAsync(rule);

            // Log
            _logger.LogInformation("Created new fraud rule: {RuleCode} - {RuleName}",
                savedRule.RuleCode, savedRule.Name);

            return savedRule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fraud rule: {Name}", model.Name);
            throw;
        }
    }

    /// <summary>
    /// Kural güncelle
    /// </summary>
    public async Task<FraudRule> UpdateRuleAsync(Guid id, FraudRuleUpdateModel model, string modifiedBy)
    {
        try
        {
            // Mevcut kuralı getir
            var existingRule = await _ruleRepository.GetRuleByIdAsync(id);
            if (existingRule == null) throw new KeyNotFoundException($"Fraud rule with ID {id} not found");

            // Model doğrulama
            //ValidateRuleModel(model);

            // Configuration JSON'a dönüştürülür
            var configJson = model.Configuration != null
                ? JsonSerializer.Serialize(model.Configuration)
                : "{}";

            // Kuralı güncelle
            existingRule.UpdateConfiguration(
                model.Name,
                model.Description,
                model.Category,
                model.Type,
                model.ImpactLevel,
                model.Actions,
                model.ActionDuration,
                model.Priority,
                model.Condition,
                configJson,
                model.ValidFrom,
                model.ValidTo,
                modifiedBy);

            // Veritabanında güncelle
            var updatedRule = await _ruleRepository.UpdateRuleAsync(existingRule);

            // Log
            _logger.LogInformation("Updated fraud rule: {RuleCode} - {RuleName}",
                updatedRule.RuleCode, updatedRule.Name);

            return updatedRule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud rule ID {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Kuralı aktifleştir
    /// </summary>
    public async Task<FraudRule> ActivateRuleAsync(Guid id, string modifiedBy)
    {
        try
        {
            // Mevcut kuralı getir
            var rule = await _ruleRepository.GetRuleByIdAsync(id);
            if (rule == null) throw new KeyNotFoundException($"Fraud rule with ID {id} not found");

            // Kuralı aktifleştir
            rule.Activate(modifiedBy);

            // Veritabanında güncelle
            var updatedRule = await _ruleRepository.UpdateRuleAsync(rule);

            // Log
            _logger.LogInformation("Activated fraud rule: {RuleCode} - {RuleName}",
                updatedRule.RuleCode, updatedRule.Name);

            return updatedRule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating fraud rule ID {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Kuralı deaktif et
    /// </summary>
    public async Task<FraudRule> DeactivateRuleAsync(Guid id, string modifiedBy)
    {
        try
        {
            // Mevcut kuralı getir
            var rule = await _ruleRepository.GetRuleByIdAsync(id);
            if (rule == null) throw new KeyNotFoundException($"Fraud rule with ID {id} not found");

            // Kuralı deaktif et
            rule.Deactivate(modifiedBy);

            // Veritabanında güncelle
            var updatedRule = await _ruleRepository.UpdateRuleAsync(rule);

            // Log
            _logger.LogInformation("Deactivated fraud rule: {RuleCode} - {RuleName}",
                updatedRule.RuleCode, updatedRule.Name);

            return updatedRule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating fraud rule ID {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Kuralı test moduna al
    /// </summary>
    public async Task<FraudRule> SetRuleTestModeAsync(Guid id, string modifiedBy)
    {
        try
        {
            // Mevcut kuralı getir
            var rule = await _ruleRepository.GetRuleByIdAsync(id);
            if (rule == null) throw new KeyNotFoundException($"Fraud rule with ID {id} not found");

            // Kuralı test moduna al
            rule.SetTestMode(modifiedBy);

            // Veritabanında güncelle
            var updatedRule = await _ruleRepository.UpdateRuleAsync(rule);

            // Log
            _logger.LogInformation("Set test mode for fraud rule: {RuleCode} - {RuleName}",
                updatedRule.RuleCode, updatedRule.Name);

            return updatedRule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting test mode for fraud rule ID {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Kural sil
    /// </summary>
    public async Task<bool> DeleteRuleAsync(Guid id)
    {
        try
        {
            // Kuralı sil (genellikle soft delete)
            var result = await _ruleRepository.DeleteRuleAsync(id);

            // Log
            if (result)
                _logger.LogInformation("Deleted fraud rule ID {RuleId}", id);
            else
                _logger.LogWarning("Failed to delete fraud rule ID {RuleId}", id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fraud rule ID {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Kurallardaki değişiklikleri uygula
    /// </summary>
    public async Task ApplyRuleChangesAsync()
    {
        try
        {
            // Cache'i yenile
            await _ruleRepository.RefreshCacheAsync();

            // Rule Engine'i yeniden yükle
            await _ruleEngine.ReloadRulesAsync();

            _logger.LogInformation("Applied rule changes and refreshed caches");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying rule changes");
            throw;
        }
    }

    /// <summary>
    /// Kural modelini doğrula
    /// </summary>
    private void ValidateRuleModel(FraudRuleCreateModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) throw new ArgumentException("Rule name cannot be empty");

        if (model.Actions == null || !model.Actions.Any())
            throw new ArgumentException("At least one action must be specified");

        // Kural tipine göre özel doğrulamalar
        if (model.Type == RuleType.Complex && string.IsNullOrWhiteSpace(model.Condition))
            throw new ArgumentException("Complex rules must have a condition expression");

        // Tipe göre özel konfigürasyon doğrulamaları yapılabilir
        switch (model.Category)
        {
            case RuleCategory.IP:
                ValidateIpBasedRuleConfig(model);
                break;
            case RuleCategory.Account:
                ValidateAccountBasedRuleConfig(model);
                break;
            case RuleCategory.Transaction:
                ValidateTransactionBasedRuleConfig(model);
                break;
        }
    }

    /// <summary>
    /// IP tabanlı kural konfigürasyonunu doğrula
    /// </summary>
    private void ValidateIpBasedRuleConfig(FraudRuleCreateModel model)
    {
        if (model.Configuration == null) throw new ArgumentException("IP based rules must have a configuration");

        // IP tabanlı kural konfigürasyonu için özel doğrulamalar
        // Örneğin, IP listesi, zaman penceresi, vb.
    }

    /// <summary>
    /// Hesap tabanlı kural konfigürasyonunu doğrula
    /// </summary>
    private void ValidateAccountBasedRuleConfig(FraudRuleCreateModel model)
    {
        if (model.Configuration == null) throw new ArgumentException("Account based rules must have a configuration");

        // Hesap tabanlı kural konfigürasyonu için özel doğrulamalar
        // Örneğin, hesap limitleri, zaman penceresi, vb.
    }

    /// <summary>
    /// İşlem tabanlı kural konfigürasyonunu doğrula
    /// </summary>
    private void ValidateTransactionBasedRuleConfig(FraudRuleCreateModel model)
    {
        if (model.Configuration == null)
            throw new ArgumentException("Transaction based rules must have a configuration");

        // İşlem tabanlı kural konfigürasyonu için özel doğrulamalar
        // Örneğin, işlem tutarı, zaman penceresi, vb.
    }
}