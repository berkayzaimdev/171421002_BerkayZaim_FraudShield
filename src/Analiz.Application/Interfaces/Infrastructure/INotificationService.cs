using Analiz.Application.DTOs.Response;
using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Infrastructure;

/// <summary>
/// Dolandırıcılık tespit sistemi bildirim servisi
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Genel uyarı bildirimi gönderir
    /// </summary>
    /// <param name="alert">Gönderilecek uyarı</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> SendAlertNotificationAsync(FraudAlert alert);

    /// <summary>
    /// Yüksek öncelikli uyarı bildirimi gönderir
    /// </summary>
    /// <param name="alert">Gönderilecek yüksek öncelikli uyarı</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> SendHighPriorityAlertAsync(FraudAlert alert);

    /// <summary>
    /// Uyarı atama bildirimi gönderir
    /// </summary>
    /// <param name="alert">Atanan uyarı</param>
    /// <param name="assignedTo">Atanan kullanıcı ID'si</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> SendAlertAssignmentAsync(FraudAlert alert, string assignedTo);

    /// <summary>
    /// Çözülen uyarı bildirimi gönderir
    /// </summary>
    /// <param name="alert">Çözülen uyarı</param>
    /// <param name="resolvedBy">Çözen kullanıcı ID'si</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> SendAlertResolutionAsync(FraudAlert alert, string resolvedBy);

    /// <summary>
    /// Toplu uyarı bildirimi gönderir
    /// </summary>
    /// <param name="alerts">Uyarı listesi</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> SendBulkAlertNotificationAsync(List<FraudAlert> alerts);

    /// <summary>
    /// Model performans düşüşü bildirimi gönderir
    /// </summary>
    /// <param name="modelName">Model adı</param>
    /// <param name="metricChange">Değişim metriği</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> SendModelPerformanceNotificationAsync(string modelName, double metricChange);

    /// <summary>
    /// Günlük uyarı özeti gönderir
    /// </summary>
    /// <param name="summary">Uyarı özeti</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> SendDailySummaryAsync(AlertSummary summary);

    /// <summary>
    /// Sistemdeki bildirim kanallarını döndürür
    /// </summary>
    /// <returns>Kanal listesi</returns>
    Task<List<NotificationChannel>> GetNotificationChannelsAsync();

    /// <summary>
    /// Kullanıcı bildirim tercihlerini getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <returns>Bildirim tercihleri</returns>
    Task<UserNotificationPreferences> GetUserNotificationPreferencesAsync(string userId);

    /// <summary>
    /// Kullanıcı bildirim tercihlerini günceller
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="preferences">Yeni tercihler</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> UpdateUserNotificationPreferencesAsync(string userId, UserNotificationPreferences preferences);
}