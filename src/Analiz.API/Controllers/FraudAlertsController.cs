// Geçici olarak devre dışı bırakıldı - IAlertService implementasyonu gerekli

using Analiz.Application.DTOs.Response;
using Analiz.Application.Interfaces.Services;
using Analiz.Application.Interfaces.Repositories;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/FraudAlerts")]
public class FraudAlertsController : ControllerBase
{
    private readonly IFraudAlertRepository _alertRepository;
    private readonly IAlertService _alertService;
    private readonly ILogger<FraudAlertsController> _logger;

    public FraudAlertsController(
        IFraudAlertRepository alertRepository,
        IAlertService alertService,
        ILogger<FraudAlertsController> logger)
    {
        _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Fraud alert'leri getirir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FraudAlertDto>), 200)]
    public async Task<IActionResult> GetFraudAlerts([FromQuery] int limit = 50)
    {
        try
        {
            _logger.LogInformation("Fraud alerts getiriliyor, limit: {Limit}", limit);
            
            var alerts = await _alertRepository.GetActiveAlertsAsync();
            
            // Limit uygula
            var limitedAlerts = alerts.Take(limit).ToList();
            
            var response = limitedAlerts.Select(alert => new FraudAlertDto
            {
                Id = alert.Id,
                TransactionId = alert.TransactionId,
                UserId = alert.UserId,
                Type = alert.Type,
                Status = alert.Status,
                RiskLevel = alert.RiskScore.Level.ToString(),
                Factors = alert.Factors,
                CreatedAt = alert.CreatedAt
            });

            _logger.LogInformation("Toplam {Count} fraud alert bulundu", limitedAlerts.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fraud alerts getirilirken hata oluştu");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Aktif fraud alert'leri getirir
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<FraudAlertDto>), 200)]
    public async Task<IActionResult> GetActiveAlerts()
    {
        try
        {
            var alerts = await _alertService.GetActiveAlertsAsync();
            
            var response = alerts.Select(alert => new FraudAlertDto
            {
                Id = alert.Id,
                TransactionId = alert.TransactionId,
                UserId = alert.UserId,
                Type = alert.Type,
                Status = alert.Status,
                RiskLevel = alert.RiskScore.Level.ToString(),
                Factors = alert.Factors,
                CreatedAt = alert.CreatedAt
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif fraud alerts getirilirken hata oluştu");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Alert sayılarını getirir
    /// </summary>
    [HttpGet("count")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetAlertCounts()
    {
        try
        {
            var activeAlerts = await _alertRepository.GetActiveAlertsAsync();
            var allAlerts = await _alertRepository.GetAllAsync();
            
            var response = new
            {
                active = activeAlerts.Count,
                total = allAlerts.Count,
                resolved = allAlerts.Count(a => a.Status == AlertStatus.Resolved),
                investigating = allAlerts.Count(a => a.Status == AlertStatus.Investigating)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert sayıları getirilirken hata oluştu");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Belirli bir alert'i getirir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FraudAlertDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAlert(Guid id)
    {
        try
        {
            var alert = await _alertRepository.GetByIdAsync(id);
            
            if (alert == null)
                return NotFound(new { Message = $"Alert {id} bulunamadı" });

            var response = new FraudAlertDto
            {
                Id = alert.Id,
                TransactionId = alert.TransactionId,
                UserId = alert.UserId,
                Type = alert.Type,
                Status = alert.Status,
                RiskLevel = alert.RiskScore.Level.ToString(),
                Factors = alert.Factors,
                CreatedAt = alert.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert {AlertId} getirilirken hata oluştu", id);
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Alert'i çözümler
    /// </summary>
    [HttpPost("{id}/resolve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResolveAlert(Guid id, [FromBody] ResolveAlertRequest request)
    {
        try
        {
            var resolvedBy = User.Identity?.Name ?? "system";
            await _alertService.ResolveAlertAsync(id, request.Resolution, resolvedBy);
            
            return Ok(new { Message = "Alert başarıyla çözüldü" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = $"Alert {id} bulunamadı" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert {AlertId} çözümlenirken hata oluştu", id);
            return BadRequest(new { Error = ex.Message });
        }
    }
}

public class ResolveAlertRequest
{
    public string Resolution { get; set; } = string.Empty;
} 