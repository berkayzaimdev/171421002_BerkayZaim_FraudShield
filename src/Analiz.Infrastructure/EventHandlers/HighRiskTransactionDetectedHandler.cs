using Analiz.Domain.Events;
using MediatR;

namespace Analiz.Infrastructure.EventHandlers;

public class HighRiskTransactionDetectedHandler : INotificationHandler<HighRiskTransactionDetectedEvent>
{
    /*  private readonly ITransactionRepository _transactionRepository;
      private readonly INotificationService _notificationService;
      private readonly ILogger<HighRiskTransactionDetectedHandler> _logger;

      public HighRiskTransactionDetectedHandler(
          ITransactionRepository transactionRepository,
          INotificationService notificationService,
          ILogger<HighRiskTransactionDetectedHandler> logger)
      {
          _transactionRepository = transactionRepository;
          _notificationService = notificationService;
          _logger = logger;
      }

      public async Task Handle(HighRiskTransactionDetectedEvent notification, CancellationToken cancellationToken)
      {
          try
          {
              _logger.LogInformation("Processing HighRiskTransactionDetected event for transaction {TransactionId}",
                  notification.TransactionId);

              // Fraud alert oluştur
              var alert = FraudAlert.Create(
                  notification.TransactionId,
                  notification.UserId,
                  notification.RiskScore,
                  notification.RiskFactors);

              // Alert'i kaydet
              await _transactionRepository.SaveFraudAlertAsync(alert);

              // İlgili taraflara bildirim gönder
              await SendNotificationsAsync(notification);
          }
          catch (Exception ex)
          {
              _logger.LogError(ex, "Error handling HighRiskTransactionDetected event for transaction {TransactionId}",
                  notification.TransactionId);
              throw;
          }
      }

      private async Task SendNotificationsAsync(HighRiskTransactionDetectedEvent notification)
      {
          // Risk seviyesine göre farklı bildirimleri gönder
          if (notification.RiskScore.Level == RiskLevel.Critical)
          {
              await _notificationService.SendCriticalRiskAlertAsync(
                  notification.TransactionId,
                  notification.UserId,
                  notification.RiskScore,
                  notification.RiskFactors);
          }
          else if (notification.RiskScore.Level == RiskLevel.High)
          {
              await _notificationService.SendHighRiskAlertAsync(
                  notification.TransactionId,
                  notification.UserId,
                  notification.RiskScore,
                  notification.RiskFactors);
          }
      }*/
    public Task Handle(HighRiskTransactionDetectedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}