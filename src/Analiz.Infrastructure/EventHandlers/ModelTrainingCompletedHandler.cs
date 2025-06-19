using Analiz.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Analiz.Infrastructure.EventHandlers;

public class ModelTrainingCompletedHandler : INotificationHandler<ModelTrainingCompletedEvent>
{
    /* private readonly IModelRepository _modelRepository;
     private readonly ILogger<ModelTrainingCompletedHandler> _logger;
     private readonly INotificationService _notificationService;

     public ModelTrainingCompletedHandler(
         IModelRepository modelRepository,
         ILogger<ModelTrainingCompletedHandler> logger,
         INotificationService notificationService)
     {
         _modelRepository = modelRepository;
         _logger = logger;
         _notificationService = notificationService;
     }

     public async Task Handle(ModelTrainingCompletedEvent notification, CancellationToken cancellationToken)
     {
         try
         {
             _logger.LogInformation("Processing ModelTrainingCompleted event for model {ModelId}",
                 notification.ModelId);

             // Model metadata'yı güncelle
             var model = await _modelRepository.GetModelAsync(notification.ModelName,
                 GetVersionFromModelId(notification.ModelId));

             if (model != null)
             {
                 model.UpdateMetrics(notification.Metrics);
                 await _modelRepository.UpdateModelAsync(model);

                 // Başarılı eğitim bildirimi gönder
                 await _notificationService.SendModelTrainingNotificationAsync(
                     notification.ModelName,
                     notification.Metrics);
             }
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Error handling ModelTrainingCompleted event for model {ModelId}",
                 notification.ModelId);
             throw;
         }
     }

     private string GetVersionFromModelId(Guid modelId)
     {
         // ModelId'den version parse etme mantığı
         return modelId.ToString("N").Substring(0, 8);
     }*/
    public Task Handle(ModelTrainingCompletedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}