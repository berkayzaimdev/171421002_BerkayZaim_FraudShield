using Analiz.Application.DTOs.Request;
using Analiz.Application.DTOs.Response;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.Rule;
using Analiz.Domain.Entities.Rule.Context;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FraudRulesController : ControllerBase
{
    private readonly IFraudRuleService _ruleService;
    private readonly ILogger<FraudRulesController> _logger;

    public FraudRulesController(
        IFraudRuleService ruleService,
        ILogger<FraudRulesController> logger)
    {
        _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm fraud kurallarını getirir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FraudRuleResponse>), 200)]
    public async Task<IActionResult> GetAllRules()
    {
        try
        {
            var rules = await _ruleService.GetAllRulesAsync();
            var response = rules.Select(MapToRuleResponse);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all fraud rules");
            return StatusCode(500, new { message = "Error retrieving fraud rules" });
        }
    }

    /// <summary>
    /// Aktif fraud kurallarını getirir
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<FraudRuleResponse>), 200)]
    public async Task<IActionResult> GetActiveRules()
    {
        try
        {
            var rules = await _ruleService.GetActiveRulesAsync();
            var response = rules.Select(MapToRuleResponse);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active fraud rules");
            return StatusCode(500, new { message = "Error retrieving active fraud rules" });
        }
    }

    /// <summary>
    /// Belirli bir kategorideki kuralları getirir
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<FraudRuleResponse>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetRulesByCategory(string category)
    {
        try
        {
            if (!Enum.TryParse<RuleCategory>(category, true, out var ruleCategory))
                return BadRequest(new { message = "Invalid rule category" });

            var rules = await _ruleService.GetRulesByCategoryAsync(ruleCategory);
            var response = rules.Select(MapToRuleResponse);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud rules for category {Category}", category);
            return StatusCode(500, new { message = "Error retrieving fraud rules by category" });
        }
    }

    /// <summary>
    /// ID'ye göre fraud kuralı getirir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FraudRuleResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRuleById(Guid id)
    {
        try
        {
            var rule = await _ruleService.GetRuleByIdAsync(id);

            if (rule == null) return NotFound(new { message = $"Fraud rule with ID {id} not found" });

            var response = MapToRuleResponse(rule);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud rule with ID {RuleId}", id);
            return StatusCode(500, new { message = "Error retrieving fraud rule" });
        }
    }

    /// <summary>
    /// Yeni bir fraud kuralı oluşturur
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,FraudManager")]
    [ProducesResponseType(typeof(FraudRuleResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateRule([FromBody] FraudRuleCreateRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Kullanıcı bilgisini al
            var username = User.Identity.Name ?? "system";

            // DTO'dan modele dönüştür
            var model = new FraudRuleCreateModel
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Type = request.Type,
                ImpactLevel = request.ImpactLevel,
                Actions = request.Actions,
                ActionDuration = request.ActionDuration,
                Priority = request.Priority,
                Condition = request.Condition,
                Configuration = request.Configuration,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo
            };

            // Kural oluştur
            var createdRule = await _ruleService.CreateRuleAsync(model, username);

            // Değişiklikleri uygula
            await _ruleService.ApplyRuleChangesAsync();

            // Yanıt oluştur
            var response = MapToRuleResponse(createdRule);

            return CreatedAtAction(nameof(GetRuleById), new { id = createdRule.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rule request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fraud rule");
            return StatusCode(500, new { message = "Error creating fraud rule" });
        }
    }

    /// <summary>
    /// Mevcut bir fraud kuralını günceller
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FraudManager")]
    [ProducesResponseType(typeof(FraudRuleResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] FraudRuleUpdateRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Kullanıcı bilgisini al
            var username = User.Identity.Name ?? "system";

            // DTO'dan modele dönüştür
            var model = new FraudRuleUpdateModel
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Type = request.Type,
                ImpactLevel = request.ImpactLevel,
                Actions = request.Actions,
                ActionDuration = request.ActionDuration,
                Priority = request.Priority,
                Condition = request.Condition,
                Configuration = request.Configuration,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo
            };

            // Kuralı güncelle
            var updatedRule = await _ruleService.UpdateRuleAsync(id, model, username);

            // Değişiklikleri uygula
            await _ruleService.ApplyRuleChangesAsync();

            // Yanıt oluştur
            var response = MapToRuleResponse(updatedRule);

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Fraud rule with ID {id} not found" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rule update request: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud rule with ID {RuleId}", id);
            return StatusCode(500, new { message = "Error updating fraud rule" });
        }
    }

    /// <summary>
    /// Bir fraud kuralını aktifleştirir
    /// </summary>
    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Admin,FraudManager")]
    [ProducesResponseType(typeof(FraudRuleResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ActivateRule(Guid id)
    {
        try
        {
            // Kullanıcı bilgisini al
            var username = User.Identity.Name ?? "system";

            // Kuralı aktifleştir
            var activatedRule = await _ruleService.ActivateRuleAsync(id, username);

            // Değişiklikleri uygula
            await _ruleService.ApplyRuleChangesAsync();

            // Yanıt oluştur
            var response = MapToRuleResponse(activatedRule);

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Fraud rule with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating fraud rule with ID {RuleId}", id);
            return StatusCode(500, new { message = "Error activating fraud rule" });
        }
    }

    /// <summary>
    /// Bir fraud kuralını deaktif eder
    /// </summary>
    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Admin,FraudManager")]
    [ProducesResponseType(typeof(FraudRuleResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeactivateRule(Guid id)
    {
        try
        {
            // Kullanıcı bilgisini al
            var username = User.Identity.Name ?? "system";

            // Kuralı deaktif et
            var deactivatedRule = await _ruleService.DeactivateRuleAsync(id, username);

            // Değişiklikleri uygula
            await _ruleService.ApplyRuleChangesAsync();

            // Yanıt oluştur
            var response = MapToRuleResponse(deactivatedRule);

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Fraud rule with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating fraud rule with ID {RuleId}", id);
            return StatusCode(500, new { message = "Error deactivating fraud rule" });
        }
    }

    /// <summary>
    /// Bir fraud kuralını test moduna alır
    /// </summary>
    [HttpPatch("{id}/test-mode")]
    [Authorize(Roles = "Admin,FraudManager")]
    [ProducesResponseType(typeof(FraudRuleResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetRuleTestMode(Guid id)
    {
        try
        {
            // Kullanıcı bilgisini al
            var username = User.Identity.Name ?? "system";

            // Kuralı test moduna al
            var testModeRule = await _ruleService.SetRuleTestModeAsync(id, username);

            // Değişiklikleri uygula
            await _ruleService.ApplyRuleChangesAsync();

            // Yanıt oluştur
            var response = MapToRuleResponse(testModeRule);

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Fraud rule with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting test mode for fraud rule with ID {RuleId}", id);
            return StatusCode(500, new { message = "Error setting test mode for fraud rule" });
        }
    }

    /// <summary>
    /// Bir fraud kuralını siler
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        try
        {
            // Kuralı sil
            var result = await _ruleService.DeleteRuleAsync(id);

            if (!result) return NotFound(new { message = $"Fraud rule with ID {id} not found" });

            // Değişiklikleri uygula
            await _ruleService.ApplyRuleChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fraud rule with ID {RuleId}", id);
            return StatusCode(500, new { message = "Error deleting fraud rule" });
        }
    }

    /// <summary>
    /// Rule entity'sini response DTO'ya dönüştürür
    /// </summary>
    private FraudRuleResponse MapToRuleResponse(FraudRule rule)
    {
        return new FraudRuleResponse
        {
            Id = rule.Id,
            RuleCode = rule.RuleCode,
            Name = rule.Name,
            Description = rule.Description,
            Category = rule.Category,
            Type = rule.Type,
            ImpactLevel = rule.ImpactLevel,
            Status = rule.Status,
            Actions = rule.Actions,
            ActionDuration = rule.ActionDuration,
            Priority = rule.Priority,
            Condition = rule.Condition,
            ConfigurationJson = rule.ConfigurationJson,
            ValidFrom = rule.ValidFrom,
            ValidTo = rule.ValidTo,
            CreatedDate = rule.CreatedAt,
            LastModified = rule.LastModified,
            ModifiedBy = rule.ModifiedBy
        };
    }
}