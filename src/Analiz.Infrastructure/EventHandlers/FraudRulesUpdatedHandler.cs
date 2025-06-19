using Analiz.Domain.Events;
using MediatR;

namespace Analiz.Infrastructure.EventHandlers;

public class FraudRulesUpdatedHandler : INotificationHandler<FraudRulesUpdatedEvent>
{
    /*private readonly ILogger<FraudRulesUpdatedHandler> _logger;
    private readonly IMemoryCache _cache;
    private readonly INotificationService _notificationService;

    public FraudRulesUpdatedHandler(
        ILogger<FraudRulesUpdatedHandler> logger,
        IMemoryCache cache,
        INotificationService notificationService)
    {
        _logger = logger;
        _cache = cache;
        _notificationService = notificationService;
    }

    public async Task Handle(FraudRulesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing FraudRulesUpdated event");

            // Cache'i temizle
            ClearRuleCache();

            // Değişiklik bildirimi gönder
            await _notificationService.SendRuleUpdateNotificationAsync(
                notification.UpdatedRules.Count,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling FraudRulesUpdated event");
            throw;
        }
    }

    private void ClearRuleCache()
    {
        var cacheKeys = _cache.GetKeys<string>()
            .Where(k => k.StartsWith("FraudRule_"));

        foreach (var key in cacheKeys)
        {
            _cache.Remove(key);
        }
    }*/
    public Task Handle(FraudRulesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}