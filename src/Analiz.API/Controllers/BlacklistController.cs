using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlacklistController : ControllerBase
{
    private readonly IBlacklistRepository _blacklistRepository;
    private readonly IBlacklistService _blacklistService;
    private readonly ILogger<BlacklistController> _logger;

    public BlacklistController(
        IBlacklistRepository blacklistRepository,
        IBlacklistService blacklistService,
        ILogger<BlacklistController> logger)
    {
        _blacklistRepository = blacklistRepository ?? throw new ArgumentNullException(nameof(blacklistRepository));
        _blacklistService = blacklistService ?? throw new ArgumentNullException(nameof(blacklistService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm kara liste öğelerini getir
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BlacklistItemResponse>>> GetBlacklistItems(
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0)
    {
        try
        {
            var items = await _blacklistRepository.GetAllAsync(limit, offset);
            var response = items.Select(ConvertToResponse).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğeleri getirilirken hata oluştu");
            return StatusCode(500, "Kara liste öğeleri getirilirken hata oluştu");
        }
    }

    /// <summary>
    /// Tip bazında kara liste öğelerini getir
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<ActionResult<List<BlacklistItemResponse>>> GetBlacklistItemsByType(BlacklistType type)
    {
        try
        {
            var items = await _blacklistRepository.GetByTypeAsync(type);
            var response = items.Select(ConvertToResponse).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tip bazında kara liste öğeleri getirilirken hata oluştu. Type: {Type}", type);
            return StatusCode(500, "Tip bazında kara liste öğeleri getirilirken hata oluştu");
        }
    }

    /// <summary>
    /// Aktif kara liste öğelerini getir
    /// </summary>
    [HttpGet("active/{type}")]
    public async Task<ActionResult<List<BlacklistItemResponse>>> GetActiveBlacklistItems(BlacklistType type)
    {
        try
        {
            var items = await _blacklistRepository.GetActiveByTypeAsync(type);
            var response = items.Select(ConvertToResponse).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif kara liste öğeleri getirilirken hata oluştu. Type: {Type}", type);
            return StatusCode(500, "Aktif kara liste öğeleri getirilirken hata oluştu");
        }
    }

    /// <summary>
    /// Kara liste özet bilgilerini getir
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<BlacklistSummary>> GetBlacklistSummary()
    {
        try
        {
            var summary = new BlacklistSummary
            {
                TotalIpCount = await _blacklistRepository.GetCountByTypeAsync(BlacklistType.IpAddress),
                ActiveIpCount = await _blacklistRepository.GetActiveCountByTypeAsync(BlacklistType.IpAddress),
                TotalAccountCount = await _blacklistRepository.GetCountByTypeAsync(BlacklistType.Account),
                ActiveAccountCount = await _blacklistRepository.GetActiveCountByTypeAsync(BlacklistType.Account),
                TotalDeviceCount = await _blacklistRepository.GetCountByTypeAsync(BlacklistType.Device),
                ActiveDeviceCount = await _blacklistRepository.GetActiveCountByTypeAsync(BlacklistType.Device),
                TotalCountryCount = await _blacklistRepository.GetCountByTypeAsync(BlacklistType.Country),
                ActiveCountryCount = await _blacklistRepository.GetActiveCountByTypeAsync(BlacklistType.Country)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste özeti getirilirken hata oluştu");
            return StatusCode(500, "Kara liste özeti getirilirken hata oluştu");
        }
    }

    /// <summary>
    /// Kara liste öğesi ekle
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BlacklistItemResponse>> AddBlacklistItem([FromBody] BlacklistItemCreateRequest request)
    {
        try
        {
            var duration = request.DurationHours.HasValue ? TimeSpan.FromHours(request.DurationHours.Value) : (TimeSpan?)null;
            
            var item = BlacklistItem.Create(
                request.Type,
                request.Value,
                request.Reason,
                duration: duration,
                addedBy: request.AddedBy ?? "system");

            var success = await _blacklistRepository.AddAsync(item);
            if (!success)
                return BadRequest("Kara liste öğesi eklenemedi");

            var response = ConvertToResponse(item);
            return CreatedAtAction(nameof(GetBlacklistItemById), new { id = item.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi eklenirken hata oluştu");
            return StatusCode(500, "Kara liste öğesi eklenirken hata oluştu");
        }
    }

    /// <summary>
    /// Kara liste öğesini ID ile getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BlacklistItemResponse>> GetBlacklistItemById(Guid id)
    {
        try
        {
            var item = await _blacklistRepository.GetByIdAsync(id);
            if (item == null)
                return NotFound();

            var response = ConvertToResponse(item);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi getirilirken hata oluştu. ID: {Id}", id);
            return StatusCode(500, "Kara liste öğesi getirilirken hata oluştu");
        }
    }

    /// <summary>
    /// Kara liste öğesini geçersiz kıl
    /// </summary>
    [HttpPatch("{id}/invalidate")]
    public async Task<ActionResult<BlacklistItemResponse>> InvalidateBlacklistItem(
        Guid id, 
        [FromBody] BlacklistInvalidateRequest request)
    {
        try
        {
            var item = await _blacklistRepository.GetByIdAsync(id);
            if (item == null)
                return NotFound();

            item.Invalidate(request.InvalidatedBy ?? "system");
            await _blacklistRepository.UpdateAsync(item);

            var response = ConvertToResponse(item);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi geçersiz kılınırken hata oluştu. ID: {Id}", id);
            return StatusCode(500, "Kara liste öğesi geçersiz kılınırken hata oluştu");
        }
    }

    /// <summary>
    /// Kara liste öğesini sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBlacklistItem(Guid id)
    {
        try
        {
            var success = await _blacklistRepository.DeleteAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste öğesi silinirken hata oluştu. ID: {Id}", id);
            return StatusCode(500, "Kara liste öğesi silinirken hata oluştu");
        }
    }

    /// <summary>
    /// Süresi dolmuş öğeleri temizle
    /// </summary>
    [HttpPost("cleanup-expired")]
    public async Task<ActionResult<int>> CleanupExpiredItems()
    {
        try
        {
            var count = await _blacklistRepository.CleanupExpiredItemsAsync();
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Süresi dolmuş kara liste öğeleri temizlenirken hata oluştu");
            return StatusCode(500, "Süresi dolmuş kara liste öğeleri temizlenirken hata oluştu");
        }
    }

    /// <summary>
    /// Kara liste kontrolü yap
    /// </summary>
    [HttpPost("check")]
    public async Task<ActionResult<bool>> CheckBlacklist([FromBody] BlacklistCheckRequest request)
    {
        try
        {
            bool isBlacklisted = request.Type switch
            {
                BlacklistType.IpAddress => await _blacklistService.IsIpBlacklistedAsync(request.Value),
                BlacklistType.Account => await _blacklistService.IsAccountBlacklistedAsync(Guid.Parse(request.Value)),
                BlacklistType.Device => await _blacklistService.IsDeviceBlacklistedAsync(request.Value),
                BlacklistType.Country => await _blacklistService.IsCountryBlacklistedAsync(request.Value),
                _ => false
            };

            return Ok(isBlacklisted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kara liste kontrolü yapılırken hata oluştu");
            return StatusCode(500, "Kara liste kontrolü yapılırken hata oluştu");
        }
    }

    private static BlacklistItemResponse ConvertToResponse(BlacklistItem item)
    {
        return new BlacklistItemResponse
        {
            Id = item.Id.ToString(),
            Type = item.Type.ToString(),
            Value = item.Value,
            Reason = item.Reason,
            Status = item.Status.ToString(),
            AddedBy = item.AddedBy,
            CreatedAt = item.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ExpiryDate = item.ExpiryDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            InvalidatedBy = item.InvalidatedBy,
            InvalidatedAt = item.InvalidatedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            IsActive = item.IsActive(),
            IsExpired = item.IsExpired()
        };
    }
}

// Response ve Request modelleri
public class BlacklistItemResponse
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public string Reason { get; set; }
    public string Status { get; set; }
    public string AddedBy { get; set; }
    public string CreatedAt { get; set; }
    public string? ExpiryDate { get; set; }
    public string? InvalidatedBy { get; set; }
    public string? InvalidatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
}

public class BlacklistItemCreateRequest
{
    public BlacklistType Type { get; set; }
    public string Value { get; set; }
    public string Reason { get; set; }
    public double? DurationHours { get; set; }
    public string? AddedBy { get; set; }
}

public class BlacklistInvalidateRequest
{
    public string? InvalidatedBy { get; set; }
}

public class BlacklistCheckRequest
{
    public BlacklistType Type { get; set; }
    public string Value { get; set; }
}

public class BlacklistSummary
{
    public int TotalIpCount { get; set; }
    public int ActiveIpCount { get; set; }
    public int TotalAccountCount { get; set; }
    public int ActiveAccountCount { get; set; }
    public int TotalDeviceCount { get; set; }
    public int ActiveDeviceCount { get; set; }
    public int TotalCountryCount { get; set; }
    public int ActiveCountryCount { get; set; }
} 