using Analiz.Application.DTOs.Request;
using Analiz.Application.DTOs.Response;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using Analiz.Domain.Models.Rule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FraudEventsController : ControllerBase
{
    private readonly IFraudRuleEventService _eventService;
    private readonly ILogger<FraudEventsController> _logger;

    public FraudEventsController(
        IFraudRuleEventService eventService,
        ILogger<FraudEventsController> logger)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm fraud olaylarını getirir
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FraudEventResponse>), 200)]
    public async Task<IActionResult> GetAllEvents()
    {
        try
        {
            var events = await _eventService.GetAllEventsAsync();
            var response = events.Select(MapToEventResponse);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all fraud events");
            return StatusCode(500, new { message = "Error retrieving fraud events" });
        }
    }

    /// <summary>
    /// Çözülmemiş fraud olaylarını getirir
    /// </summary>
    [HttpGet("unresolved")]
    [ProducesResponseType(typeof(IEnumerable<FraudEventResponse>), 200)]
    public async Task<IActionResult> GetUnresolvedEvents()
    {
        try
        {
            var events = await _eventService.GetUnresolvedEventsAsync();
            var response = events.Select(MapToEventResponse);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unresolved fraud events");
            return StatusCode(500, new { message = "Error retrieving unresolved fraud events" });
        }
    }

    /// <summary>
    /// Hesap ID'sine göre fraud olaylarını getirir
    /// </summary>
    [HttpGet("account/{accountId}")]
    [ProducesResponseType(typeof(IEnumerable<FraudEventResponse>), 200)]
    public async Task<IActionResult> GetEventsByAccountId(Guid accountId)
    {
        try
        {
            var events = await _eventService.GetEventsByAccountIdAsync(accountId);
            var response = events.Select(MapToEventResponse);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud events for account {AccountId}", accountId);
            return StatusCode(500, new { message = "Error retrieving fraud events by account" });
        }
    }

    /// <summary>
    /// IP adresine göre fraud olaylarını getirir
    /// </summary>
    [HttpGet("ip/{ipAddress}")]
    [ProducesResponseType(typeof(IEnumerable<FraudEventResponse>), 200)]
    public async Task<IActionResult> GetEventsByIpAddress(string ipAddress)
    {
        try
        {
            var events = await _eventService.GetEventsByIpAddressAsync(ipAddress);
            var response = events.Select(MapToEventResponse);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud events for IP {IpAddress}", ipAddress);
            return StatusCode(500, new { message = "Error retrieving fraud events by IP" });
        }
    }

    /// <summary>
    /// ID'ye göre fraud olayını getirir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FraudEventResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        try
        {
            var fraudEvent = await _eventService.GetEventByIdAsync(id);

            if (fraudEvent == null) return NotFound(new { message = $"Fraud event with ID {id} not found" });

            var response = MapToEventResponse(fraudEvent);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud event with ID {EventId}", id);
            return StatusCode(500, new { message = "Error retrieving fraud event" });
        }
    }

    /// <summary>
    /// Bir fraud olayını çözer/kapatır
    /// </summary>
    [HttpPatch("{id}/resolve")]
    [Authorize(Roles = "Admin,FraudManager,FraudAnalyst")]
    [ProducesResponseType(typeof(FraudEventResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResolveEvent(Guid id, [FromBody] FraudEventResolveRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Kullanıcı bilgisini al
            var username = User.Identity.Name ?? "system";

            // DTO'dan modele dönüştür
            var model = new FraudEventResolveModel
            {
                Status = request.Status,
                ResolutionNotes = request.ResolutionNotes
            };

            // Olayı çöz
            var resolvedEvent = await _eventService.ResolveEventAsync(id, model, username);

            // Yanıt oluştur
            var response = MapToEventResponse(resolvedEvent);

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Fraud event with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving fraud event with ID {EventId}", id);
            return StatusCode(500, new { message = "Error resolving fraud event" });
        }
    }

    /// <summary>
    /// Event entity'sini response DTO'ya dönüştürür
    /// </summary>
    private FraudEventResponse MapToEventResponse(FraudRuleEvent fraudEvent)
    {
        return new FraudEventResponse
        {
            Id = fraudEvent.Id,
            RuleId = fraudEvent.RuleId,
            RuleName = fraudEvent.RuleName,
            RuleCode = fraudEvent.RuleCode,
            TransactionId = fraudEvent.TransactionId,
            AccountId = fraudEvent.AccountId,
            IpAddress = fraudEvent.IpAddress,
            DeviceInfo = fraudEvent.DeviceInfo,
            Actions = fraudEvent.Actions,
            ActionDuration = fraudEvent.ActionDuration,
            ActionEndDate = fraudEvent.ActionEndDate,
            EventDetailsJson = fraudEvent.EventDetailsJson,
            CreatedDate = fraudEvent.CreatedAt,
            ResolvedDate = fraudEvent.ResolvedDate,
            ResolvedBy = fraudEvent.ResolvedBy,
            ResolutionNotes = fraudEvent.ResolutionNotes
        };
    }
}