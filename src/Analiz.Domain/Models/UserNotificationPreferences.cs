using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

/// <summary>
/// Kullanıcı bildirim tercihleri
/// </summary>
public class UserNotificationPreferences
{
    /// <summary>
    /// Kullanıcı ID'si
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Email bildirimleri aktif mi
    /// </summary>
    public bool EmailEnabled { get; set; }

    /// <summary>
    /// SMS bildirimleri aktif mi
    /// </summary>
    public bool SmsEnabled { get; set; }

    /// <summary>
    /// Push bildirimleri aktif mi
    /// </summary>
    public bool PushEnabled { get; set; }

    /// <summary>
    /// Uygulama içi bildirimleri aktif mi
    /// </summary>
    public bool InAppEnabled { get; set; }


    /// <summary>
    /// Günlük özet bildirimleri aktif mi
    /// </summary>
    public bool DailySummaryEnabled { get; set; }

    /// <summary>
    /// Abonelik listesi (hangi uyarı tiplerinde bildirim alınacak)
    /// </summary>
    public Dictionary<string, bool> SubscriptionList { get; set; }
}