namespace FraudShield.TransactionAnalysis.Domain.Enums;

/// <summary>
/// Bildirim kanalı tipleri
/// </summary>
public enum NotificationChannelType
{
    Email,
    SMS,
    PushNotification,
    InApp,
    Webhook,
    Slack,
    Teams
}