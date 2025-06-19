using Analiz.Application.DTOs.Request;
using Analiz.Application.DTOs.Response;
using Analiz.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FraudDetectionController : ControllerBase
{
    private readonly IFraudDetectionService _detectionService;
    private readonly ILogger<FraudDetectionController> _logger;

    public FraudDetectionController(
        IFraudDetectionService detectionService,
        ILogger<FraudDetectionController> logger)
    {
        _detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// İşlemi analiz eder ve hibrit yaklaşımla fraud kontrolü yapar
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeTransaction([FromBody] TransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _logger.LogInformation("Received transaction analysis request for user {UserId}", request.UserId);

            // Analiz et
            var response = await _detectionService.AnalyzeTransactionAsync(request);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing transaction for user {UserId}", request.UserId);
            return StatusCode(500, new { message = "Error analyzing transaction" });
        }
    }

    #region Single Context Checks

    /// <summary>
    /// İşlem fraud kontrolü yapar
    /// </summary>
    [HttpPost("check-transaction")]
    [ProducesResponseType(typeof(FraudDetectionResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CheckTransaction([FromBody] TransactionCheckRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Fraud kontrolü yap
            var result = await _detectionService.CheckTransactionAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking transaction for fraud: {TransactionId}", request.TransactionId);
            return StatusCode(500, new { message = "Error checking transaction for fraud" });
        }
    }

    /// <summary>
    /// Hesap erişimi fraud kontrolü yapar
    /// </summary>
    [HttpPost("check-account-access")]
    [ProducesResponseType(typeof(FraudDetectionResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CheckAccountAccess([FromBody] AccountAccessCheckRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Fraud kontrolü yap
            var result = await _detectionService.CheckAccountAccessAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking account access for fraud: {AccountId}", request.AccountId);
            return StatusCode(500, new { message = "Error checking account access for fraud" });
        }
    }

    /// <summary>
    /// IP adresi fraud kontrolü yapar
    /// </summary>
    [HttpPost("check-ip")]
    [ProducesResponseType(typeof(FraudDetectionResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CheckIpAddress([FromBody] IpCheckRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Fraud kontrolü yap
            var result = await _detectionService.CheckIpAddressAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking IP address for fraud: {IpAddress}", request.IpAddress);
            return StatusCode(500, new { message = "Error checking IP address for fraud" });
        }
    }

    /// <summary>
    /// Oturum fraud kontrolü yapar
    /// </summary>
    [HttpPost("check-session")]
    [ProducesResponseType(typeof(FraudDetectionResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CheckSession([FromBody] SessionCheckRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Fraud kontrolü yap
            var result = await _detectionService.CheckSessionAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session for fraud: {SessionId}", request.SessionId);
            return StatusCode(500, new { message = "Error checking session for fraud" });
        }
    }

    /// <summary>
    /// Cihaz fraud kontrolü yapar
    /// </summary>
    [HttpPost("check-device")]
    [ProducesResponseType(typeof(FraudDetectionResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CheckDevice([FromBody] DeviceCheckRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Fraud kontrolü yap
            var result = await _detectionService.CheckDeviceAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking device for fraud: {DeviceId}", request.DeviceId);
            return StatusCode(500, new { message = "Error checking device for fraud" });
        }
    }

    #endregion

    #region ML Model Evaluation

    /// <summary>
    /// Sadece ML modeli ile değerlendirme yapar
    /// </summary>
    [HttpPost("evaluate-model")]
    [ProducesResponseType(typeof(ModelEvaluationResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> EvaluateWithModel([FromBody] ModelEvaluationRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // ML modeli değerlendirmesi yap
            var result = await _detectionService.EvaluateWithModelOnlyAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating with ML model: {TransactionId}", request.TransactionId);
            return StatusCode(500, new { message = "Error evaluating with ML model" });
        }
    }

    #endregion

    #region Comprehensive Fraud Check

    /// <summary>
    /// Kapsamlı dolandırıcılık kontrolü yapar
    /// </summary>
    [HttpPost("comprehensive-check")]
    [ProducesResponseType(typeof(ComprehensiveFraudCheckResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> PerformComprehensiveCheck([FromBody] ComprehensiveFraudCheckRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Kapsamlı dolandırıcılık kontrolü yap
            var result = await _detectionService.PerformComprehensiveCheckAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing comprehensive fraud check");
            return StatusCode(500, new { message = "Error performing comprehensive fraud check" });
        }
    }

    #endregion
}