using System.Text.Json;
using Analiz.Application.Interfaces.Test;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestTransactionController : ControllerBase
{
    private readonly ITransactionTestService _transactionTestService;
    private readonly ILogger<TestTransactionController> _logger;

    public TestTransactionController(
        ITransactionTestService transactionTestService,
        ILogger<TestTransactionController> logger)
    {
        _transactionTestService = transactionTestService;
        _logger = logger;
    }

    /// <summary>
    /// Belirtilen sayıda test işlemi oluşturur
    /// </summary>
    /// <param name="count">Oluşturulacak işlem sayısı</param>
    /// <param name="fraudPercentage">Dolandırıcılık işlemlerinin yüzdesi (0-100 arası)</param>
    /// <returns>Oluşturulan işlemler</returns>
    [HttpGet("generate")]
    public IActionResult GenerateTransactions(
        [FromQuery] int count = 10,
        [FromQuery] double fraudPercentage = 0.2)
    {
        _logger.LogInformation("Test işlemleri oluşturuluyor: {Count} adet, {FraudPercentage}% dolandırıcılık",
            count, fraudPercentage);

        try
        {
            var transactions = _transactionTestService.GenerateTransactions(count, fraudPercentage);

            return Ok(new
            {
                Success = true,
                TransactionCount = transactions.Count,
                Transactions = transactions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem oluşturulurken hata oluştu");
            return StatusCode(500, new { Error = "İşlem oluşturulurken bir hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// Kredi kartı veri setinden gerçek değerlerle işlemler oluşturur
    /// </summary>
    /// <param name="count">Oluşturulacak işlem sayısı</param>
    /// <returns>Oluşturulan işlemler</returns>
    [HttpGet("generate-from-dataset")]
    public async Task<IActionResult> GenerateTransactionsFromDataset([FromQuery] int count = 10)
    {
        _logger.LogInformation("Veri setinden test işlemleri oluşturuluyor: {Count} adet", count);

        try
        {
            var transactions = await _transactionTestService.GenerateTransactionsFromDatasetAsync(count);

            return Ok(new
            {
                Success = true,
                TransactionCount = transactions.Count,
                Transactions = transactions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veri setinden işlem oluşturulurken hata oluştu");
            return StatusCode(500, new { Error = "İşlem oluşturulurken bir hata oluştu", Message = ex.Message });
        }
    }

    /// <summary>
    /// Belirtilen sayıda test işlemi oluşturur ve JSON dosyasına kaydeder
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportTransactions(
        [FromQuery] int count = 10,
        [FromQuery] double fraudPercentage = 0.2)
    {
        _logger.LogInformation(
            "Test işlemleri oluşturuluyor ve dışa aktarılıyor: {Count} adet, {FraudPercentage}% dolandırıcılık",
            count, fraudPercentage);

        try
        {
            var transactions = _transactionTestService.GenerateTransactions(count, fraudPercentage);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(transactions, options);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            return File(bytes, "application/json", $"test-transactions-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem oluşturulurken hata oluştu");
            return StatusCode(500, new { Error = "İşlem oluşturulurken bir hata oluştu", Message = ex.Message });
        }
    }
}