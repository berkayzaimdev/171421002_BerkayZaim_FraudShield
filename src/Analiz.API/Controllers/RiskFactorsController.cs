using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RiskFactorsController : ControllerBase
{
    private readonly IRiskFactorRepository _riskFactorRepository;
    private readonly ILogger<RiskFactorsController> _logger;

    public RiskFactorsController(
        IRiskFactorRepository riskFactorRepository,
        ILogger<RiskFactorsController> logger)
    {
        _riskFactorRepository = riskFactorRepository ?? throw new ArgumentNullException(nameof(riskFactorRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm risk faktörlerini getirir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public async Task<IActionResult> GetAllRiskFactors([FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        try
        {
            var riskFactors = await _riskFactorRepository.GetAllAsync();
            
            var result = riskFactors
                .Skip(offset)
                .Take(limit)
                .Select(rf => new
                {
                    rf.Id,
                    rf.TransactionId,
                    rf.Code,
                    Type = rf.Type.ToString(),
                    rf.Description,
                    rf.Confidence,
                    Severity = rf.Severity.ToString(),
                    rf.AnalysisResultId,
                    rf.RuleId,
                    rf.ActionTaken,
                    rf.Source,
                    rf.DetectedAt
                });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk factors");
            return StatusCode(500, new { message = "Error retrieving risk factors" });
        }
    }

    /// <summary>
    /// Risk faktörlerini çeşitli kriterlere göre filtreler
    /// </summary>
    [HttpGet("filtered")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public async Task<IActionResult> GetFilteredRiskFactors(
        [FromQuery] string? type = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0)
    {
        try
        {
            var riskFactors = await _riskFactorRepository.GetAllAsync();
            
            // Filtreleme
            var filtered = riskFactors.AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                filtered = filtered.Where(rf => rf.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(severity))
            {
                filtered = filtered.Where(rf => rf.Severity.ToString().Equals(severity, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(source))
            {
                filtered = filtered.Where(rf => rf.Source.Equals(source, StringComparison.OrdinalIgnoreCase));
            }

            if (startDate.HasValue)
            {
                filtered = filtered.Where(rf => rf.DetectedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filtered = filtered.Where(rf => rf.DetectedAt <= endDate.Value);
            }

            var result = filtered
                .Skip(offset)
                .Take(limit)
                .Select(rf => new
                {
                    rf.Id,
                    rf.TransactionId,
                    rf.Code,
                    Type = rf.Type.ToString(),
                    rf.Description,
                    rf.Confidence,
                    Severity = rf.Severity.ToString(),
                    rf.AnalysisResultId,
                    rf.RuleId,
                    rf.ActionTaken,
                    rf.Source,
                    rf.DetectedAt
                });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered risk factors");
            return StatusCode(500, new { message = "Error retrieving filtered risk factors" });
        }
    }

    /// <summary>
    /// Belirli bir işlem için risk faktörlerini getirir
    /// </summary>
    [HttpGet("transaction/{transactionId}")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public async Task<IActionResult> GetRiskFactorsByTransaction(Guid transactionId)
    {
        try
        {
            var riskFactors = await _riskFactorRepository.GetByTransactionIdAsync(transactionId.ToString());
            
            var result = riskFactors.Select(rf => new
            {
                rf.Id,
                rf.TransactionId,
                rf.Code,
                Type = rf.Type.ToString(),
                rf.Description,
                rf.Confidence,
                Severity = rf.Severity.ToString(),
                rf.AnalysisResultId,
                rf.RuleId,
                rf.ActionTaken,
                rf.Source,
                rf.DetectedAt
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk factors for transaction {TransactionId}", transactionId);
            return StatusCode(500, new { message = "Error retrieving risk factors for transaction" });
        }
    }

    /// <summary>
    /// Risk faktörü detayını getirir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRiskFactorById(Guid id)
    {
        try
        {
            var riskFactor = await _riskFactorRepository.GetByIdAsync(id);

            if (riskFactor == null)
                return NotFound(new { message = $"Risk factor with ID {id} not found" });

            var result = new
            {
                riskFactor.Id,
                riskFactor.TransactionId,
                riskFactor.Code,
                Type = riskFactor.Type.ToString(),
                riskFactor.Description,
                riskFactor.Confidence,
                Severity = riskFactor.Severity.ToString(),
                riskFactor.AnalysisResultId,
                riskFactor.RuleId,
                riskFactor.ActionTaken,
                riskFactor.Source,
                riskFactor.DetectedAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk factor with ID {RiskFactorId}", id);
            return StatusCode(500, new { message = "Error retrieving risk factor" });
        }
    }

    /// <summary>
    /// Risk faktörlerinin özet istatistiklerini getirir
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetRiskFactorSummary()
    {
        try
        {
            var riskFactors = await _riskFactorRepository.GetAllAsync();
            
            var summary = new
            {
                TotalCount = riskFactors.Count(),
                CriticalCount = riskFactors.Count(rf => rf.Severity == FraudShield.TransactionAnalysis.Domain.Enums.RiskLevel.Critical),
                HighCount = riskFactors.Count(rf => rf.Severity == FraudShield.TransactionAnalysis.Domain.Enums.RiskLevel.High),
                MediumCount = riskFactors.Count(rf => rf.Severity == FraudShield.TransactionAnalysis.Domain.Enums.RiskLevel.Medium),
                LowCount = riskFactors.Count(rf => rf.Severity == FraudShield.TransactionAnalysis.Domain.Enums.RiskLevel.Low),
                TypeDistribution = riskFactors
                    .GroupBy(rf => rf.Type.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                SourceDistribution = riskFactors
                    .GroupBy(rf => rf.Source)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageConfidence = riskFactors.Any() ? riskFactors.Average(rf => rf.Confidence) : 0
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk factor summary");
            return StatusCode(500, new { message = "Error retrieving risk factor summary" });
        }
    }
} 