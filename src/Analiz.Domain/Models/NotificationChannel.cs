using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

/// <summary>
/// Bildirim kanalı
/// </summary>
public class NotificationChannel
{
    /// <summary>
    /// Kanal ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Kanal adı
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Kanal tipi
    /// </summary>
    public NotificationChannelType Type { get; set; }

    /// <summary>
    /// Kanal aktif mi
    /// </summary>
    public bool IsActive { get; set; }
}