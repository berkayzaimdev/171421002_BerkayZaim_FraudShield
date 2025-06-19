using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using Analiz.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionAnalysisController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAnalysisResultRepository _analysisResultRepository;
    private readonly IRiskFactorRepository _riskFactorRepository;
    private readonly ILogger<TransactionAnalysisController> _logger;

    public TransactionAnalysisController(
        ITransactionRepository transactionRepository,
        IAnalysisResultRepository analysisResultRepository,
        IRiskFactorRepository riskFactorRepository,
        ILogger<TransactionAnalysisController> logger)
    {
        _transactionRepository = transactionRepository;
        _analysisResultRepository = analysisResultRepository;
        _riskFactorRepository = riskFactorRepository;
        _logger = logger;
    }

    /// <summary>
    /// İşlemin tam detaylarını getirir (Transaction, AnalysisResult, RiskFactors)
    /// </summary>
    [HttpGet("complete-details/{transactionId}")]
    public async Task<IActionResult> GetTransactionCompleteDetails(Guid transactionId)
    {
        try
        {
            _logger.LogInformation("İşlem tam detayları getiriliyor: {TransactionId}", transactionId);

            // 1. Transaction bilgilerini al
            var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
            
            if (transaction == null)
            {
                return NotFound(new { Error = "İşlem bulunamadı", TransactionId = transactionId });
            }

            // 2. AnalysisResult bilgilerini al
            var analysisResult = await _analysisResultRepository.GetByTransactionIdAsync(transaction.Id);

            // 3. RiskFactors bilgilerini al
            var riskFactors = new List<object>();
            if (analysisResult != null)
            {
                var riskFactorEntities = await _riskFactorRepository.GetByAnalysisResultIdAsync(analysisResult.Id);
                riskFactors = riskFactorEntities.Select(rf => new
                {
                    Id = rf.Id,
                    Code = rf.Code,
                    Description = rf.Description,
                    Confidence = rf.Confidence,
                    Severity = rf.Severity.ToString(),
                    Source = rf.Source,
                    RuleId = rf.RuleId,
                    ActionTaken = rf.ActionTaken,
                    Type = rf.Type.ToString(),
                    DetectedAt = rf.DetectedAt
                }).ToList<object>();
            }

            // Response'u oluştur
            var response = new
            {
                Transaction = new
                {
                    Id = transaction.Id,
                    TransactionId = transaction.Id,
                    UserId = transaction.UserId,
                    Amount = transaction.Amount,
                    TransactionTime = transaction.TransactionTime,
                    Type = transaction.Type.ToString(),
                    Status = transaction.Status.ToString(),
                    MerchantId = transaction.MerchantId,
                    DeviceInfo = transaction.DeviceInfo,
                    Location = transaction.Location
                },
                AnalysisResult = analysisResult != null ? new
                {
                    Id = analysisResult.Id,
                    TransactionId = analysisResult.TransactionId,
                    FraudProbability = analysisResult.FraudProbability,
                    RiskScore = analysisResult.RiskScore,
                    RiskLevel = analysisResult.RiskScore?.Level.ToString(),
                    AnomalyScore = analysisResult.AnomalyScore,
                    Decision = analysisResult.Decision.ToString(),
                    AnalyzedAt = analysisResult.AnalyzedAt,
                    Status = analysisResult.Status.ToString()
                } : null,
                RiskFactors = riskFactors
            };

            return Ok(new
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem tam detayları getirilirken hata oluştu: {TransactionId}", transactionId);
            return StatusCode(500, new { Error = "İşlem detayları getirilirken hata oluştu", Message = ex.Message });
        }
    }
} 